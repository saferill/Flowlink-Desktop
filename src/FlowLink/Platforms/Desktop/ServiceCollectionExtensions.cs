using FlowLink.Data.Contracts;
using FlowLink.Platforms.Desktop.Bluetooth;
using FlowLink.Platforms.Desktop.Services;

namespace FlowLink.Platforms.Desktop;

/// <summary>
/// Extension methods for registering Desktop-specific services
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {

        services.AddSingleton<IPlatformNotificationHandler, DesktopNotificationHandler>();
        services.AddSingleton<IMediaService, DesktopMediaService>();
        services.AddSingleton<IBatteryService, DesktopBatteryService>();
        services.AddSingleton<IActionService, DesktopActionService>();
        services.AddSingleton<IUpdateService, DesktopUpdateService>();
        services.AddSingleton<ISftpService, DesktopSftpService>();
        services.AddSingleton<IAppShortcutService, DesktopAppShortcutService>();
        services.AddSingleton<IPhoneLineService, DesktopPhoneLineService>();
        services.AddSingleton<IBluetoothPairingService, BluetoothPairingService>();
        services.AddSingleton<BluetoothPairingService>(sp => (BluetoothPairingService)sp.GetRequiredService<IBluetoothPairingService>());
        return services;
    }
} 
