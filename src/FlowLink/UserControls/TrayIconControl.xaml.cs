using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using FlowLink.Data.Contracts;
using FlowLink.Data.Models;
#if WINDOWS
using FlowLink.Platforms.Windows.Interop;
#endif
using Windows.UI.ViewManagement;

namespace FlowLink.UserControls;

[ObservableObject]
public sealed partial class TrayIconControl : UserControl
{
    private readonly UISettings uiSettings = new();
    private IDeviceManager DeviceManager { get; } = Ioc.Default.GetRequiredService<IDeviceManager>();
    private INetworkService NetworkService { get; } = Ioc.Default.GetRequiredService<INetworkService>();
    private ISessionManager SessionManager { get; } = Ioc.Default.GetRequiredService<ISessionManager>();

    [ObservableProperty]
    private string connectionStatusText = "FlowLink";

    public TrayIconControl()
    {
        InitializeComponent();

        try
        {
            TrayIcon.ForceCreate();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TrayIcon] ForceCreate error: {ex.Message}");
        }

        // Set initial icon based on current theme
        UpdateTrayIcon(uiSettings);

        // Monitor system theme changes
        uiSettings.ColorValuesChanged += UpdateTrayIcon;

        // Subscribe to connection status changes (ISessionManager has the event)
        SessionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
        DeviceManager.ActiveDeviceChanged += OnActiveDeviceChanged;

        // Set initial status
        UpdateConnectionStatus(DeviceManager.ActiveDevice);
    }

    // ─── Tray Icon Commands ─────────────────────────────────────────────────────

    /// <summary>Opens the main window and brings it to the foreground.</summary>
    [RelayCommand]
    public void OpenWindow()
    {
#if WINDOWS || __DESKTOP__
        App.ShowMainWindow();
#endif
    }

    /// <summary>
    /// Disconnects all active devices and triggers auto-reconnect.
    /// Useful when network changes or connection is stale.
    /// </summary>
    [RelayCommand]
    public void RestartConnection()
    {
        try
        {
            foreach (var device in DeviceManager.PairedDevices.ToList())
            {
                if (device.IsConnected)
                    SessionManager.DisconnectDevice(device, forcedDisconnect: false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TrayIcon] RestartConnection error: {ex.Message}");
        }
    }

    /// <summary>Gracefully exits the application, stopping all services.</summary>
    [RelayCommand]
    public void ExitApplication()
    {
        // Stop all auto-reconnect background tasks first
        NetworkService.StopAllReconnects();

        // Disable close event interception so the window actually closes
        App.HandleClosedEvents = false;

        // Clean up tray icon
        TrayIcon.Dispose();

        // Close window and exit app
        App.MainWindow?.Close();
        App.Current.Exit();

        // Force termination if needed (handles edge cases in Uno runtime)
        Process.GetCurrentProcess().Kill();
    }

    // ─── Connection Status ──────────────────────────────────────────────────────

    private void OnConnectionStatusChanged(object? sender, PairedDevice device)
        => UpdateConnectionStatus(device);

    private void OnActiveDeviceChanged(object? sender, PairedDevice? device)
        => UpdateConnectionStatus(device);

    private void UpdateConnectionStatus(PairedDevice? device)
    {
        var dq = DispatcherQueue ?? App.MainWindow?.DispatcherQueue;
        dq?.TryEnqueue(() =>
        {
            try
            {
                if (device is not null && device.IsConnected)
                {
                    var template = "ConnectionStatus.Connected".GetLocalizedResource();
                    ConnectionStatusText = string.Format(template, device.Name);
                }
                else
                {
                    ConnectionStatusText = "ConnectionStatus.Disconnected".GetLocalizedResource();
                }

                // Update tray tooltip directly
                TrayIcon.ToolTipText = ConnectionStatusText;
            }
            catch { }
        });
    }

    // ─── Theme Icon ─────────────────────────────────────────────────────────────

    private void UpdateTrayIcon(UISettings sender, object? args = null)
    {
        try
        {
            var iconPath = sender.GetColorValue(UIColorType.Background) == Colors.Black
                ? "ms-appx:///Assets/Icons/FlowLinkDark.ico"
                : "ms-appx:///Assets/Icons/FlowLinkLight.ico";

            var dq = DispatcherQueue ?? App.MainWindow?.DispatcherQueue;
            dq?.TryEnqueue(() =>
            {
                try
                {
                    TrayIcon.IconSource = new BitmapImage(new(iconPath));
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to detect theme: {ex.Message}");
        }
    }
}
