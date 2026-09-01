using Microsoft.UI.Xaml.Media.Imaging;
using FlowLink.Data.Models;

namespace FlowLink.Data.Contracts;

public interface IDiscoveryService
{
    /// <summary>
    /// Starts the udp discovery process.
    /// </summary>
    Task StartDiscoveryAsync();

    void StopDiscovery();

    /// <summary>
    /// Gets the current UDP broadcast data.
    /// </summary>
    UdpBroadcast? BroadcastMessage { get; }

    /// <summary>
    /// Generates a QR code image for device connection.
    /// </summary>
    Task<BitmapImage?> GenerateQrCodeAsync();
}
