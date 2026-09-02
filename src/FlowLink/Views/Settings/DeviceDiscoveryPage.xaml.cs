using FlowLink.Data.Contracts;
using FlowLink.Data.Items;
using FlowLink.Data.Models;
using FlowLink.Helpers;
using FlowLink.Utils.Serialization;

namespace FlowLink.Views.Settings;

public sealed partial class DeviceDiscoveryPage : Page
{
    private readonly ISessionManager SessionManager = Ioc.Default.GetRequiredService<ISessionManager>();
    private readonly IDiscoveryService DiscoveryService = Ioc.Default.GetRequiredService<IDiscoveryService>();

    public DeviceDiscoveryPage()
    {
        InitializeComponent();
        SetupBreadcrumb();
        Loaded += DeviceDiscoveryPage_Loaded;
        Unloaded += DeviceDiscoveryPage_Unloaded;
    }

    private async void DeviceDiscoveryPage_Loaded(object sender, RoutedEventArgs e)
    {
        await DiscoveryService.StartDiscoveryAsync();
    }

    private void DeviceDiscoveryPage_Unloaded(object sender, RoutedEventArgs e)
    {
        DiscoveryService.StopDiscovery();
    }

    private void SetupBreadcrumb()
    {
        BreadcrumbBar.ItemsSource = new ObservableCollection<BreadcrumbBarItemModel>
        {
            new("Devices".GetLocalizedResource(), typeof(DevicesPage)),
            new("AvailableDevices/Title".GetLocalizedResource(), typeof(DeviceDiscoveryPage))
        };
        BreadcrumbBar.ItemClicked += BreadcrumbBar_ItemClicked;
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        var items = BreadcrumbBar.ItemsSource as ObservableCollection<BreadcrumbBarItemModel>;
        var clickedItem = items?[args.Index];
        
        if (clickedItem?.PageType is not null && clickedItem.PageType != typeof(DeviceDiscoveryPage))
        {
            // Navigate back to devices page
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }


    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DiscoveredDevice device)
        {
            SessionManager.Pair(device);
        }
    }

    private async void QrCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var bitmapImage = await DiscoveryService.GenerateQrCodeAsync();
        
        if (bitmapImage is null)
        {
            QrCodeImage.Source = null;
            return;
        }

        QrCodeImage.Source = bitmapImage;
        QrCodeStatusText.Text = $"Scan this QR code to connect";
    }

    private void DirectIpConnect_Click(object sender, RoutedEventArgs e)
    {
        var ip = DirectIpTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(ip))
        {
            SessionManager.ConnectTo(ip, ip, 5150);
            var device = new DiscoveredDevice
            {
                Id = ip,
                Name = $"Device ({ip})",
                Address = ip,
                Port = 5150,
                VerificationKey = $"Tailscale: {ip}"
            };
            SessionManager.Pair(device);
        }
    }
}

