using FlowLink.Data.AppDatabase;
using FlowLink.Data.AppDatabase.Repository;
using FlowLink.Data.Contracts;
using FlowLink.Models;
#if WINDOWS
using FlowLink.Platforms.Windows;
#else
using FlowLink.Platforms.Desktop;
#endif
using FlowLink.Services;
using FlowLink.Services.FileTransfer;
using FlowLink.Services.Settings;
using FlowLink.Services.Socket;
using FlowLink.ViewModels;
using FlowLink.ViewModels.Settings;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FlowLink.Helpers;

/// <summary>
/// Provides static helper to manage app lifecycle.
/// </summary>
public static class AppLifecycleHelper
{
    /// <summary>
    /// Gets application package version.
    /// </summary>
    public static Version AppVersion { get; } =
        new(Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

    public static async Task InitializeAppComponentsAsync()
    {
        var discoveryService = Ioc.Default.GetRequiredService<IDiscoveryService>();
        var clipboardService = Ioc.Default.GetRequiredService<IClipboardService>();
        var networkService = Ioc.Default.GetRequiredService<INetworkService>();
        var notificationService = Ioc.Default.GetRequiredService<INotificationService>();
        var deviceManager = Ioc.Default.GetRequiredService<IDeviceManager>();
        var adbService = Ioc.Default.GetRequiredService<IAdbService>();
        var playbackService = Ioc.Default.GetRequiredService<IMediaService>();
        var actionService = Ioc.Default.GetRequiredService<IActionService>();
        var updateService = Ioc.Default.GetRequiredService<IUpdateService>();
        var phoneLineService = Ioc.Default.GetRequiredService<IPhoneLineService>();
        var callManager = Ioc.Default.GetRequiredService<ICallManager>();
        var batteryService = Ioc.Default.GetRequiredService<IBatteryService>();
#if WINDOWS
        var windowsNotificationHandler = Ioc.Default.GetRequiredService<IPlatformNotificationHandler>();
        await windowsNotificationHandler.RegisterForNotifications();
#endif

        await deviceManager.Initialize();

        await Task.WhenAll(
            playbackService.InitializeAsync(),
            actionService.InitializeAsync(),
            notificationService.Initialize(),
            clipboardService.Initialize(),
            batteryService.InitializeAsync(),
            callManager.Initialize()
        );

        await networkService.StartServerAsync();

        await Task.WhenAll(
            adbService.StartAsync(),
            updateService.CheckForUpdatesAsync(),
            phoneLineService.Initialize()
        );

        App.SplashScreenLoadingTCS?.TrySetResult();
    }

    public static IApplicationBuilder ConfigureApp(this App app, LaunchActivatedEventArgs args)
    {
        return app.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Debug :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                }, enableUnoLogging: true)
                .UseSerilog(
                    consoleLoggingEnabled: true,
                    fileLoggingEnabled: true,
                    configureLogger: config =>
                    {
                        config.WriteTo.File(
                            Path.Combine(ApplicationData.Current.LocalFolder.Path, "Logs", "Log_.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7
                        );
                    }
                )
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .UseLocalization()
                .ConfigureServices((context, services) => services

                .AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<App>>())

                // Settings Services
                .AddSingleton<IUserSettingsService, UserSettingsService>()
                .AddSingleton<IGeneralSettingsService, GeneralSettingsService>(sp => new GeneralSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
                .AddSingleton<IAppThemeModeService, AppThemeModeService>()

                // Database and Repositories
                .AddSingleton<DatabaseContext>()
                .AddSingleton<DeviceRepository>()
                .AddSingleton<RemoteAppRepository>()
                .AddSingleton<ContactRepository>()
                .AddSingleton<SmsRepository>()
                .AddSingleton<CallLogRepository>()
                .AddSingleton<NotificationRepository>()

                // Platform-specific services
#if WINDOWS
                .AddWindowsServices()
#else
                .AddDesktopServices()
#endif
                // Services
                .AddSingleton<IDeviceManager, DeviceManager>()
                .AddSingleton(sp => (ITcpServerProvider)sp.GetRequiredService<INetworkService>())
                .AddSingleton(sp => (ISessionManager)sp.GetRequiredService<INetworkService>())
                .AddSingleton<IMdnsService, MdnsService>()
                .AddSingleton<IDiscoveryService, DiscoveryService>()
                .AddSingleton<INetworkService, NetworkService>()

                .AddSingleton<INotificationService, NotificationService>()
                .AddSingleton<IBatteryAlertService, BatteryAlertService>()
                .AddSingleton<IClipboardService, ClipboardService>()
                .AddSingleton<IRemoteMediaHandler, RemoteMediaHandler>()
                .AddSingleton<SmsHandlerService>()
                .AddSingleton<ICallHandler, CallHandlerService>()
                .AddSingleton<ICallManager>(sp => (CallHandlerService)sp.GetRequiredService<ICallHandler>())

                .AddSingleton<IMessageHandler, MessageHandler>()
                .AddSingleton<Lazy<IMessageHandler>>(sp => new Lazy<IMessageHandler>(() => sp.GetRequiredService<IMessageHandler>()))
                .AddSingleton<IAdbService, AdbService>()
                .AddSingleton<IScreenMirrorService, ScreenMirrorService>()
                .AddSingleton<IFileTransferService, FileTransferService>()

                // ViewModels
                .AddSingleton<MainPageViewModel>()
                .AddSingleton<DevicesViewModel>()
                .AddSingleton<AppsViewModel>()
                .AddSingleton<MessagesViewModel>()
                .AddSingleton<CallsPageViewModel>()
                )
            );
    }

    /// <summary>
    /// Shows exception on the Debug Output.
    /// </summary>
    public static void HandleAppUnhandledException(Exception? ex)
    {
        Ioc.Default.GetService<ILogger>()?.LogCritical("Unhandled exception {ex}", ex);
    }

    public static async Task HandleStartupTaskAsync(bool enable)
    {
#if WINDOWS
        try
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("8B5D3E3F-9B69-4E8A-A9F7-BFCA793B9AF0");

            if (enable)
            {
                if (startupTask.State is Windows.ApplicationModel.StartupTaskState.Disabled)
                    await startupTask.RequestEnableAsync();
            }
            else
            {
                if (startupTask.State is Windows.ApplicationModel.StartupTaskState.Enabled)
                    startupTask.Disable();
            }
        }
        catch
        {
            // Fallback for unpackaged app using Registry Run key
            try
            {
                const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKeyPath, true);
                if (key != null)
                {
                    if (enable)
                    {
                        string exePath = Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            key.SetValue("FlowLink", $"\"{exePath}\" --startup");
                        }
                    }
                    else
                    {
                        key.DeleteValue("FlowLink", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to register startup task in registry: {ex}", ex);
            }
        }

        // Register Windows Explorer Right-Click "Send to Phone (FlowLink)" Context Menu
        RegisterWindowsContextMenu(enable);
#endif
    }

    public static void RegisterWindowsContextMenu(bool enable)
    {
#if WINDOWS
        try
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? "";
            if (string.IsNullOrEmpty(exePath) || !System.IO.File.Exists(exePath)) return;

            // 1. Context Menu for all files: HKCU\Software\Classes\*\shell\FlowLinkSend
            const string fileShellKey = @"Software\Classes\*\shell\FlowLinkSend";
            if (enable)
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(fileShellKey, true);
                if (key != null)
                {
                    key.SetValue("", "Send to Phone (FlowLink)");
                    key.SetValue("Icon", $"\"{exePath}\",0");
                    using var cmdKey = key.CreateSubKey("command", true);
                    cmdKey?.SetValue("", $"\"{exePath}\" --send \"%1\"");
                }
            }
            else
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(fileShellKey, false);
            }

            // 2. Context Menu for directories: HKCU\Software\Classes\Directory\shell\FlowLinkSend
            const string dirShellKey = @"Software\Classes\Directory\shell\FlowLinkSend";
            if (enable)
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(dirShellKey, true);
                if (key != null)
                {
                    key.SetValue("", "Send to Phone (FlowLink)");
                    key.SetValue("Icon", $"\"{exePath}\",0");
                    using var cmdKey = key.CreateSubKey("command", true);
                    cmdKey?.SetValue("", $"\"{exePath}\" --send \"%1\"");
                }
            }
            else
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(dirShellKey, false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppLifecycleHelper] Failed to register context menu: {ex.Message}");
        }
#endif
    }
}
