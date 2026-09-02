using Microsoft.UI.Xaml.Media.Animation;
using FlowLink.Data.Contracts;
using FlowLink.Data.Models;
using FlowLink.Helpers;
using FlowLink.Utils.Serialization;
using FlowLink.ViewModels.Settings;

namespace FlowLink.Views.Onboarding;

public sealed partial class SyncPage : Page
{
    public DevicesViewModel ViewModel { get; }

    private readonly ISessionManager SessionManager = Ioc.Default.GetRequiredService<ISessionManager>();
    private readonly IDiscoveryService DiscoveryService = Ioc.Default.GetRequiredService<IDiscoveryService>();

    public SyncPage()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<DevicesViewModel>();
        Loaded += SyncPage_Loaded;
        Unloaded += SyncPage_Unloaded;
    }

    private async void SyncPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var networkService = Ioc.Default.GetRequiredService<INetworkService>();
            await networkService.StartServerAsync();
        }
        catch { }
        await DiscoveryService.StartDiscoveryAsync();
        await GenerateQrCodeAsync();
    }

    private void SyncPage_Unloaded(object sender, RoutedEventArgs e)
    {
        DiscoveryService.StopDiscovery();
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        // Mark onboarding as completed
        ApplicationData.Current.LocalSettings.Values["HasCompletedOnboarding"] = true;
        Frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
    }

    private async Task GenerateQrCodeAsync()
    {
        try
        {
            var bitmapImage = await DiscoveryService.GenerateQrCodeAsync();
            if (bitmapImage is not null)
            {
                QrCodeImage.Source = bitmapImage;
                QrCodeImage.Visibility = Visibility.Visible;    
            }
            else
            {
                QrCodeImage.Source = null;
                QrCodeImage.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            QrCodeImage.Source = null;
            QrCodeImage.Visibility = Visibility.Collapsed;
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DiscoveredDevice device)
        {
            SessionManager.Pair(device);
        }
    }

    private void DirectConnectButton_Click(object sender, RoutedEventArgs e)
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
