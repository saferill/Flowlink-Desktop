using System.Net;
using System.Text;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml.Media.Imaging;
using FlowLink.Data.AppDatabase.Models;
using FlowLink.Data.Contracts;
using FlowLink.Data.EventArguments;
using FlowLink.Data.Models;
using FlowLink.Helpers;
using FlowLink.Services.Socket;
using FlowLink.Utils;
using FlowLink.Utils.Serialization;

namespace FlowLink.Services;

public class DiscoveryService(
    ILogger logger,
    IMdnsService mdnsService,
    IDeviceManager deviceManager,
    ISessionManager sessionManager
    ) : IDiscoveryService, IUdpClientProvider
{
    private MulticastClient? udpClient; 
    private const string DEFAULT_BROADCAST = "255.255.255.255";
    private LocalDeviceEntity? localDevice;
    private readonly int port = 5149;
    private List<IPEndPoint> broadcastEndpoints = [];
    private const int DiscoveryPort = 5149;
    private static readonly IEnumerable<int> PORT_RANGE = Enumerable.Range(5150, 20);

    private CancellationTokenSource? tailscaleDiscoveryCts;

    public UdpBroadcast? BroadcastMessage { get; private set; }

    public async Task StartDiscoveryAsync()
    {
        try
        {
            localDevice = await deviceManager.GetLocalDeviceAsync();
            var localAddresses = NetworkHelper.GetAllValidAddresses();

            var name = await UserInformation.GetCurrentUserNameAsync();
            BroadcastMessage = new UdpBroadcast
            {
                DeviceId = localDevice.DeviceId,
                DeviceName = name,
                Port = NetworkService.ServerPort
            };

            mdnsService.AdvertiseService(BroadcastMessage, port);
            mdnsService.StartDiscovery();
            mdnsService.DiscoveredMdnsService += OnDiscoveredMdnsService;

            // Start Tailscale Auto-Discovery Loop
            StartTailscaleDiscovery();

            broadcastEndpoints = [.. localAddresses.Select(ipInfo =>
            {
                var network = new Data.Models.IPNetwork(ipInfo.Address, ipInfo.SubnetMask);
                var broadcastAddress = network.BroadcastAddress;

                // Fallback to gateway if broadcast is limited
                return broadcastAddress.Equals(IPAddress.Broadcast) && ipInfo.Gateway is not null
                    ? new IPEndPoint(ipInfo.Gateway, DiscoveryPort)
                    : new IPEndPoint(broadcastAddress, DiscoveryPort);

            }).Distinct()];

            // Always include default broadcast as fallback
            broadcastEndpoints.Add(new IPEndPoint(IPAddress.Parse(DEFAULT_BROADCAST), DiscoveryPort));

            var addresses = deviceManager.GetRemoteDeviceAddresses();
            broadcastEndpoints.AddRange(addresses.Select(address => new IPEndPoint(IPAddress.Parse(address), DiscoveryPort)));

            logger.Info($"Active broadcast endpoints: {string.Join(", ", broadcastEndpoints)}");

            udpClient = new MulticastClient("0.0.0.0", port, this, logger)
            {
                OptionDualMode = false,
                OptionMulticast = true,
                OptionReuseAddress = true,
            };
            udpClient.SetupMulticast(true);

            if (udpClient.Connect())
            {
                udpClient.Socket.EnableBroadcast = true;
                logger.Info($"UDP Client connected successfully {port}");
                BroadcastDeviceInfoAsync(BroadcastMessage);
            }
            else
            {
                logger.Error("Failed to connect UDP client");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Discovery initialization failed: {ex.Message}", ex);
        }
    }

    private void OnDiscoveredMdnsService(object? sender, DiscoveredMdnsServiceArgs e)
    {
        sessionManager.ConnectTo(e.DeviceId, e.Address, e.Port);
    }

    private async void BroadcastDeviceInfoAsync(UdpBroadcast udpBroadcast)
    {
        if (udpClient is null || udpBroadcast is null) return;
        
        string jsonMessage = JsonMessageSerializer.Serialize(udpBroadcast);
        byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
        foreach (var endPoint in broadcastEndpoints)
        {
            try
            {
                udpClient.Socket.SendTo(messageBytes, endPoint);
            }
            catch
            {
                // ignore
            }
        }
    }

    public async void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        try
        {
            var message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            var address = ((IPEndPoint)endpoint).Address;
            if (JsonMessageSerializer.DeserializeMessage(message) is not UdpBroadcast broadcast) return;

            if (broadcast.DeviceId == localDevice?.DeviceId || address is null) return;

            sessionManager.ConnectTo(broadcast.DeviceId, address.ToString(), broadcast.Port);
        }
        catch (Exception ex)
        {
            logger.Warn($"Error processing UDP message: {ex.Message}", ex);
        }
    }

    public void StopDiscovery()
    {
        try
        {
            tailscaleDiscoveryCts?.Cancel();
            tailscaleDiscoveryCts?.Dispose();
            tailscaleDiscoveryCts = null;

            mdnsService.DiscoveredMdnsService -= OnDiscoveredMdnsService;
            mdnsService.UnAdvertiseService();
            udpClient?.Dispose();
            udpClient = null;
        }
        catch (Exception ex)
        {
            logger.Error($"Error disposing default UDP client: {ex.Message}", ex);
        }
    }

    public async Task<BitmapImage?> GenerateQrCodeAsync()
    {
        try
        {
            var broadcast = BroadcastMessage;
            if (broadcast is null)
            {
                return null;
            }

            var localAddresses = NetworkHelper.GetAllValidAddresses();
            var addresses = localAddresses.Select(addr => addr.Address.ToString()).ToList();

            var connectionInfo = new
            {
                Addresses = addresses,
                broadcast.Port,
                broadcast.DeviceId,
                broadcast.DeviceName
            };

            var jsonData = JsonMessageSerializer.Serialize(connectionInfo);

            var qrCodeBytes = ImageHelper.GenerateQrCode(jsonData);
            if (qrCodeBytes is null)
            {
                return null;
            }

            return await qrCodeBytes.ToBitmapAsync(256);
        }
        catch (Exception ex)
        {
            logger.Warn($"Error generating QR code: {ex.Message}", ex);
            return null;
        }
    }

    private void StartTailscaleDiscovery()
    {
        tailscaleDiscoveryCts = new CancellationTokenSource();
        var token = tailscaleDiscoveryCts.Token;

        Task.Run(async () =>
        {
            // Initial grace period after startup/reboot to allow Windows network and Tailscale services to initialize
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(4), token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            logger.Info("Starting Tailscale Auto-Discovery loop (Adaptive 3s Fast Polling)...");
            while (!token.IsCancellationRequested)
            {
                bool isAnyDeviceConnected = false;
                try
                {
                    isAnyDeviceConnected = deviceManager.PairedDevices.Any(d => d.IsConnected);
                    
                    // Always scan and attempt connection if not yet connected
                    if (!isAnyDeviceConnected)
                    {
                        await ScanAndConnectTailscalePeersAsync();
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn($"Error in Tailscale Auto-Discovery scan: {ex.Message}");
                }

                // If not connected, search aggressively every 3 seconds; once connected, idle check every 20 seconds
                var delaySeconds = isAnyDeviceConnected ? 20 : 3;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private async Task ScanAndConnectTailscalePeersAsync()
    {
        // 1. Check known paired devices with Tailscale IPs
        foreach (var pairedDevice in deviceManager.PairedDevices)
        {
            if (pairedDevice.IsConnected) continue;

            var tailscaleAddress = pairedDevice.Addresses.FirstOrDefault(a => a.Address.StartsWith("100."));
            if (tailscaleAddress != null)
            {
                sessionManager.ConnectTo(tailscaleAddress.Address, tailscaleAddress.Address, pairedDevice.Port);
            }
        }

        // 2. Discover active Android Tailscale peers via Tailscale CLI
        var startInfo = new ProcessStartInfo
        {
            FileName = "tailscale",
            Arguments = "status --json",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        string output = "";
        try
        {
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
            }
        }
        catch
        {
            // Ignore failure if not in PATH, will retry common paths below
        }

        // If tailscale isn't in PATH or command failed, try common installation paths
        if (string.IsNullOrWhiteSpace(output))
        {
            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Tailscale\tailscale.exe"),
                @"C:\Program Files\Tailscale\tailscale.exe"
            };

            foreach (var path in commonPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        startInfo.FileName = path;
                        using var retryProcess = Process.Start(startInfo);
                        if (retryProcess != null)
                        {
                            output = await retryProcess.StandardOutput.ReadToEndAsync();
                            await retryProcess.WaitForExitAsync();
                            if (retryProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore and try next path
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(output)) return;

        try
        {
            using var doc = JsonDocument.Parse(output);
            if (!doc.RootElement.TryGetProperty("Peer", out var peerElement) || peerElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var peerProperty in peerElement.EnumerateObject())
            {
                var peer = peerProperty.Value;
                
                bool isOnline = peer.TryGetProperty("Online", out var onlineProp) && onlineProp.GetBoolean();
                if (!isOnline) continue;

                string os = peer.TryGetProperty("OS", out var osProp) ? osProp.GetString() ?? "" : "";
                string hostName = peer.TryGetProperty("HostName", out var hProp) ? hProp.GetString() ?? "Android Device" : "Android Device";
                string peerId = peer.TryGetProperty("ID", out var idProp) ? idProp.GetString() ?? "" : "";

                if (peer.TryGetProperty("TailscaleIPs", out var ipsProp) && ipsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ipElement in ipsProp.EnumerateArray())
                    {
                        string? ip = ipElement.GetString();
                        if (string.IsNullOrEmpty(ip)) continue;

                        // Only support IPv4 for simplicity
                        if (ip.Contains(':')) continue; 

                        // 1. Direct UDP broadcast announcement to phone's FlowLink port (5149)
                        if (BroadcastMessage != null && udpClient != null && IPAddress.TryParse(ip, out var targetIp))
                        {
                            try
                            {
                                string json = JsonMessageSerializer.Serialize(BroadcastMessage);
                                byte[] bytes = Encoding.UTF8.GetBytes(json);
                                udpClient.Socket.SendTo(bytes, new IPEndPoint(targetIp, DiscoveryPort));
                                
                                lock (broadcastEndpoints)
                                {
                                    var ep = new IPEndPoint(targetIp, DiscoveryPort);
                                    if (!broadcastEndpoints.Any(e => e.Address.Equals(targetIp) && e.Port == DiscoveryPort))
                                    {
                                        broadcastEndpoints.Add(ep);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Warn($"Failed to send Tailscale direct UDP to {ip}: {ex.Message}");
                            }
                        }

                        // 2. Add directly to UI DiscoveredDevices list so it appears immediately on SyncPage and AvailableDevices
                        string targetId = string.IsNullOrEmpty(peerId) ? ip : peerId;
                        if (!deviceManager.PairedDevices.Any(d => d.Addresses.Any(a => a.Address == ip)) &&
                            !deviceManager.DiscoveredDevices.Any(d => d.Address == ip || d.Id == targetId))
                        {
                            var discovered = new DiscoveredDevice
                            {
                                Id = targetId,
                                Name = $"{hostName} (Tailscale)",
                                Address = ip,
                                Port = 5150,
                                VerificationKey = $"Tailscale: {ip}"
                            };
                            if (App.MainWindow?.DispatcherQueue != null)
                            {
                                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                                {
                                    if (!deviceManager.DiscoveredDevices.Any(d => d.Address == ip || d.Id == discovered.Id))
                                    {
                                        deviceManager.DiscoveredDevices.Add(discovered);
                                    }
                                });
                            }
                        }

                        // 3. Proactively initiate TLS connection to Android across all server ports (5149 to 5169)
                        sessionManager.ConnectTo(ip, ip, DiscoveryPort);
                        foreach (int port in PORT_RANGE)
                        {
                            sessionManager.ConnectTo(ip, ip, port);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warn($"Failed to parse Tailscale JSON status: {ex.Message}");
        }
    }
}
