using System.Net.Sockets;
using System.Text;
using FlowLink.Data.Contracts;
using FlowLink.Data.Models;
using FlowLink.Helpers;
using FlowLink.Services.Socket;

namespace FlowLink.Services.FileTransfer;

public partial class ReceiveFileHandler(
    List<FileMetadata> files,
    ServerInfo serverInfo,
    PairedDevice device,
    byte[] expectedCert,
    string storageLocation,
    ILogger logger,
    IPlatformNotificationHandler notificationHandler) : ITcpClientProvider, IDisposable
{
    private Client? client;
    private FileStream? fileStream;
    private FileMetadata? currentFileMetadata;
    private long bytesTransferred;
    private long totalBytesTransferred = 0;
    private readonly long totalBytes = files.Sum(f => f.FileSize);
    private int currentFileIndex = 0;
    private uint notificationSequence = 1;
    private TaskCompletionSource<bool>? handshakeTcs;
    private TaskCompletionSource<bool>? transferCompletionSource;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private bool disposed;

    public Guid TransferId { get; private set; }
    public double Progress => (double)totalBytesTransferred / totalBytes * 100;
    public bool IsBulkTransfer => files.Count > 1;

    private List<string> GetCandidateAddresses()
    {
        var candidates = new List<string>();

        if (device.Client != null && device.Client.IsConnected && !string.IsNullOrWhiteSpace(device.Client.Address))
        {
            candidates.Add(device.Client.Address);
        }

        if (device.Session != null && device.Session.IsConnected && device.Session.Socket.RemoteEndPoint is System.Net.IPEndPoint remoteEp)
        {
            var sessionIp = remoteEp.Address.ToString();
            if (!candidates.Contains(sessionIp))
                candidates.Add(sessionIp);
        }

        if (!string.IsNullOrWhiteSpace(device.Address) && !candidates.Contains(device.Address))
        {
            candidates.Add(device.Address);
        }

        foreach (var addr in device.GetEnabledAddresses())
        {
            if (!string.IsNullOrWhiteSpace(addr) && !candidates.Contains(addr))
            {
                candidates.Add(addr);
            }
        }

        return candidates;
    }

    /// <summary>
    /// Connects to the file transfer server and authenticates.
    /// </summary>
    /// <returns>The transfer ID for tracking this transfer.</returns>
    public async Task<Guid> ConnectAsync()
    {
        var clientContext = SslHelper.CreateSslContext(expectedCert);
        var candidates = GetCandidateAddresses();

        if (candidates.Count == 0)
            throw new IOException($"No target address available for device {device.Name} ({device.Id})");

        Exception? lastException = null;

        foreach (var address in candidates)
        {
            try
            {
                logger.Info($"Attempting file transfer connection to {address}:{serverInfo.Port} for device {device.Name}");
                client = new Client(clientContext, address, serverInfo.Port, this);
                TransferId = client.Id;

                if (!client.ConnectAsync())
                {
                    logger.Warn($"Failed to initiate socket connection to {address}:{serverInfo.Port}");
                    continue;
                }

                // Wait for TLS handshake
                if (!client.IsHandshaked)
                {
                    handshakeTcs = new TaskCompletionSource<bool>();
                    await handshakeTcs.Task.WaitAsync(TimeSpan.FromSeconds(8));
                }

                logger.Info($"Successfully connected to file transfer server at {address}:{serverInfo.Port}");
                return TransferId;
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed file transfer connection to {address}:{serverInfo.Port}: {ex.Message}");
                lastException = ex;
                try { client?.DisconnectAsync(); } catch { }
            }
        }

        throw new IOException($"Failed to connect to file transfer server on any address. Last error: {lastException?.Message}", lastException);
    }

    /// <summary>
    /// Receives files from the connected server.
    /// </summary>
    /// <returns>The received file for single file transfers, null for bulk.</returns>
    public async Task<StorageFile?> ReceiveAsync()
    {
        StorageFile? resultFile = null;
        
        try
        {
            // Show initial notification
            ShowProgressNotification();

            // Process each file
            foreach (var fileMetadata in files)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                logger.Info($"Starting to receive file {currentFileIndex + 1}/{files.Count}: {fileMetadata.FileName}");

                // Wait for previous file to complete (for bulk transfers)
                if (transferCompletionSource?.Task is { IsCompleted: false })
                {
                    await transferCompletionSource.Task;
                }

                string fullPath = Path.Combine(storageLocation, fileMetadata.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                transferCompletionSource = new TaskCompletionSource<bool>();
                currentFileMetadata = fileMetadata;
                fileStream = new FileStream(fullPath, FileMode.Create);

                var startMessage = Encoding.UTF8.GetBytes(FileTransferService.StartMessage + "\n");
                client?.Send(startMessage);

                // Wait for this file transfer to complete
                await transferCompletionSource.Task;

                currentFileIndex++;
                logger.Info($"Received file {fileMetadata.FileName}");

                // For single file transfer, capture the result
                if (!IsBulkTransfer)
                {
                    resultFile = await StorageFile.GetFileFromPathAsync(fullPath);
                }

                CleanupFileStream();
            }

            // Show completion notification
            if (IsBulkTransfer)
            {
                notificationHandler.ShowCompletedFileTransferNotification(
                    string.Format("FileTransferNotification.CompletedBulk".GetLocalizedResource(), files.Count, device.Name),
                    TransferId.ToString(),
                    folderPath: storageLocation);
            }
            else
            {
                notificationHandler.ShowCompletedFileTransferNotification(
                    string.Format("FileTransferNotification.CompletedSingle".GetLocalizedResource(), files[0].FileName, device.Name),
                    TransferId.ToString(),
                    Path.Combine(storageLocation, files[0].FileName));
            }

            logger.Info($"File transfer completed: {currentFileIndex}/{files.Count} files received");
        }
        catch (OperationCanceledException)
        {
            logger.Info("File transfer cancelled");
            CleanupFailedFile();
        }
        catch (Exception ex)
        {
            logger.Error("Error during file transfer", ex);
            CleanupFailedFile();
        }

        return resultFile;
    }

    public void Cancel()
    {
        cancellationTokenSource.Cancel();
    }

    private void ShowProgressNotification()
    {
        var fileName = currentFileMetadata?.FileName ?? files[0].FileName;

        // Title: fileName (index/total)
        var progressTitle = $"{fileName} ({currentFileIndex + 1}/{files.Count})";

        // Subtitle: Receiving/Sending message
        var notificationTitle = IsBulkTransfer
            ? string.Format("FileTransferNotification.ReceivingBulk".GetLocalizedResource(), files.Count, device.Name)
            : string.Format("FileTransferNotification.Receiving".GetLocalizedResource(), device.Name);

        // Status: "{transferred} / {total}"
        var transferredFormatted = FormatBytes(totalBytesTransferred);
        var totalFormatted = FormatBytes(totalBytes);
        var status = $"{transferredFormatted} / {totalFormatted}";

        notificationHandler.ShowFileTransferNotification(
            notificationTitle,
            progressTitle,
            status,
            TransferId.ToString(),
            notificationSequence,
            Progress);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private void CleanupFileStream()
    {
        fileStream?.Close();
        fileStream?.Dispose();
        fileStream = null;
        currentFileMetadata = null;
        bytesTransferred = 0;
    }

    private void CleanupFailedFile()
    {
        notificationHandler.RemoveNotificationByTag(TransferId.ToString());
        
        // Save file metadata before cleanup
        var fileToDelete = currentFileMetadata;
        
        CleanupFileStream();
        
        // Delete the incomplete file if it exists
        if (fileToDelete is not null)
        {
            var failedFilePath = Path.Combine(storageLocation, fileToDelete.FileName);
            if (File.Exists(failedFilePath))
            {
                try
                {
                    File.Delete(failedFilePath);
                    logger.Info($"Deleted incomplete file: {fileToDelete.FileName}");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to delete incomplete file: {fileToDelete.FileName}", ex);
                }
            }
        }
    }

    #region ITcpClientProvider Implementation

    public void OnConnected(Client client)
    {
        logger.Info("Connected to file transfer server");
    }

    public void OnDisconnected(Client client)
    {
        logger.Info("Disconnected from file transfer server");

        handshakeTcs?.TrySetException(new IOException("Disconnected before TLS handshake completed"));

        // If transfer is not complete
        if (currentFileMetadata is not null && fileStream is not null && bytesTransferred < currentFileMetadata.FileSize)
        {
            transferCompletionSource?.TrySetException(new IOException("Connection to server lost"));
        }
    }

    public void OnError(Client client, SocketError error)
    {
        logger.Error($"Socket error occurred during file transfer: {error}");
        handshakeTcs?.TrySetException(new IOException($"Socket error before TLS handshake completed: {error}"));
        transferCompletionSource?.TrySetException(new IOException($"Socket error: {error}"));
    }

    public void OnHandshaked(Client client)
    {
        handshakeTcs?.TrySetResult(true);
    }

    private long lastNotificationTime;

    public void OnReceived(Client client, byte[] buffer, long offset, long size)
    {
        try
        {
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (fileStream is null || currentFileMetadata is null) return;

            fileStream.Write(buffer, (int)offset, (int)size);
            bytesTransferred += size;
            totalBytesTransferred += size;

            long now = Environment.TickCount64;
            if (now - lastNotificationTime > 150 || totalBytesTransferred >= totalBytes)
            {
                lastNotificationTime = now;
                notificationSequence++;
                ShowProgressNotification();
            }

            if (bytesTransferred >= currentFileMetadata.FileSize)
            {
                logger.Info($"File {currentFileMetadata.FileName} received successfully");
                client.Send(Encoding.UTF8.GetBytes(FileTransferService.CompleteMessage + "\n"));
                bytesTransferred = 0;
                transferCompletionSource?.TrySetResult(true);
            }
        }
        catch (Exception ex)
        {
            transferCompletionSource?.TrySetException(ex);
        }
    }

    #endregion

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        CleanupFileStream();
        client?.DisconnectAsync();
        client?.Dispose();
        client = null;
        handshakeTcs = null;
        transferCompletionSource = null;
        cancellationTokenSource.Dispose();
        
        GC.SuppressFinalize(this);
    }
}

