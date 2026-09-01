using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.AppLifecycle;
using FlowLink.Helpers;
using FlowLink.Views;
using FlowLink.Views.Onboarding;
using Windows.ApplicationModel.Activation;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using H.NotifyIcon;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using FlowLink.Data.Models;
using FlowLink.Views.WindowViews;

#warning CHECKING CONSTANTS
#if WINDOWS
#warning WINDOWS is defined!
#endif
#if __DESKTOP__
#warning __DESKTOP__ is defined!
#endif

#if WINDOWS
using FlowLink.Platforms.Windows.Helpers;
#endif

namespace FlowLink;
public partial class App : Application
{
    public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }
    public static bool HandleClosedEvents { get; set; } = true;
    public static UserControls.TrayIconControl? TrayIcon { get; private set; }
    public static Window MainWindow { get; private set; } = null!;
    protected IHost? Host { get; private set; }

    // Track open DeviceSettingsWindow instances
    private static readonly Dictionary<string, DeviceSettingsWindow> DeviceSettingsWindows = [];

    public App()
    {
        InitializeComponent();
        // Configure exception handlers
        UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = ActivateAsync();

        async Task ActivateAsync()
        {
            var builder = this.ConfigureApp(args);
            MainWindow = builder.Window;
            MainWindow.AppWindow.Title = "FlowLink";
            MainWindow.SetWindowIcon();
#if WINDOWS || __DESKTOP__
            MainWindow.ExtendsContentIntoTitleBar = true;
#endif
#if DEBUG
            MainWindow.UseStudio();
#endif
            Host = builder.Build();
            Ioc.Default.ConfigureServices(Host.Services);
            await Host.StartAsync();

            // Initialize system tray icon globally
            TrayIcon = new UserControls.TrayIconControl();

            bool isStartupTask = false;
#if WINDOWS || __DESKTOP__
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
                    isStartupTask = appActivationArguments.Data is IStartupTaskActivatedEventArgs ||
                                    Array.Exists(Environment.GetCommandLineArgs(), arg => arg.Equals("--startup", StringComparison.OrdinalIgnoreCase) || arg.Equals("-startup", StringComparison.OrdinalIgnoreCase));

                    if (appActivationArguments.Data is ProtocolActivatedEventArgs protocolArgs)
                        HandleProtocolActivationArgs(protocolArgs);
                }
                catch
                {
                    isStartupTask = Array.Exists(Environment.GetCommandLineArgs(), arg => arg.Equals("--startup", StringComparison.OrdinalIgnoreCase) || arg.Equals("-startup", StringComparison.OrdinalIgnoreCase));
                }

                HookEventsForWindow();
                bool isStartupRegistered = ApplicationData.Current.LocalSettings.Values["isStartupRegistered"] == null;
                if (isStartupRegistered)
                {
                    await AppLifecycleHelper.HandleStartupTaskAsync(true);
                    ApplicationData.Current.LocalSettings.Values["isStartupRegistered"] = true;
                }
            }
#endif
            var rootFrame = EnsureWindowIsInitialized();
            if (rootFrame is null)
                return;

            Ioc.Default.GetRequiredService<IAppThemeModeService>().ManageAppearance(MainWindow);

            if (isStartupTask)
            {
                var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
                var startupOption = userSettingsService.GeneralSettingsService.StartupOption;
                switch (startupOption)
                {
                    case StartupOptions.InTray:
                        // Don't activate or show the window
                        break;
                    case StartupOptions.Minimized:
                        // Need to show the window first, then minimize it
                        MainWindow.Activate();
                        await Task.Delay(200);
                        OverlappedPresenter overlappedPresenter = (MainWindow.AppWindow.Presenter as OverlappedPresenter) ?? OverlappedPresenter.Create();
                        if (overlappedPresenter.IsMinimizable)
                        {
                            overlappedPresenter.Minimize();
                        }
                        break;
                    default:
                        MainWindow.Activate();
                        MainWindow.AppWindow.Show();
                        break;
                };
            }
            else
            {
                MainWindow.Activate();
                // Wait for the Window to initialize
                await Task.Delay(10);
                MainWindow.AppWindow.Show();
            }

            rootFrame.Navigate(typeof(Views.SplashScreen));

            SplashScreenLoadingTCS = new TaskCompletionSource();
            await SplashScreenLoadingTCS!.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
            SplashScreenLoadingTCS = null;

            await AppLifecycleHelper.InitializeAppComponentsAsync();

            // Handle Right-Click "Send to Phone (FlowLink)" and command line file arguments
            var cmdArgs = Environment.GetCommandLineArgs();
            var filePathsToSend = new List<string>();
            for (int i = 1; i < cmdArgs.Length; i++)
            {
                var arg = cmdArgs[i];
                if (arg.Equals("--startup", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-startup", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--send", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-send", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (System.IO.File.Exists(arg))
                {
                    filePathsToSend.Add(arg);
                }
            }

            if (filePathsToSend.Count > 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var storageItems = new List<IStorageItem>();
                        foreach (var path in filePathsToSend)
                        {
                            var storageFile = await StorageFile.GetFileFromPathAsync(path);
                            storageItems.Add(storageFile);
                        }

                        if (storageItems.Count > 0)
                        {
                            var fileTransferService = Ioc.Default.GetRequiredService<IFileTransferService>();
                            fileTransferService.SendFilesWithPicker(storageItems);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App] Error sending files from command line: {ex.Message}");
                    }
                });
            }

            bool isOnboarding = ApplicationData.Current.LocalSettings.Values["HasCompletedOnboarding"] == null;
            if (isOnboarding)
            {
                // Navigate to onboarding page
                rootFrame.Navigate(typeof(WelcomePage), null, new SuppressNavigationTransitionInfo());
            }
            else
            {
                // Navigate to main page
                rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
            }
        }
    }

    public Frame? EnsureWindowIsInitialized()
    {
        try
        {
            if (MainWindow.Content is not Microsoft.UI.Xaml.Controls.Grid mainGrid)
            {
                var rootFrame = new Frame() { CacheSize = 1 };
                rootFrame.NavigationFailed += OnNavigationFailed;

                mainGrid = new Microsoft.UI.Xaml.Controls.Grid();
                mainGrid.Children.Add(rootFrame);

                if (TrayIcon != null)
                {
                    mainGrid.Children.Add(TrayIcon);
                }

                MainWindow.Content = mainGrid;
            }

            if (mainGrid.Children[0] is Frame frame)
            {
                return frame;
            }
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }


#if WINDOWS || __DESKTOP__

    /// <summary>
    /// Gets invoked when the application is activated.
    /// </summary>
    public async Task OnActivatedAsync(AppActivationArguments activatedEventArgs)
    {
        // InitializeApplication accesses UI, needs to be called on UI thread
        await MainWindow.DispatcherQueue.EnqueueAsync(() => InitializeApplicationAsync(activatedEventArgs));
    }

    /// <summary>Parses flowlink://&lt;package&gt; and launches scrcpy for that package.</summary>
    private static async void HandleProtocolActivationArgs(ProtocolActivatedEventArgs protocolArgs)
    {
        var package = protocolArgs.Uri.Host;
        if (string.IsNullOrEmpty(package)) return;
        var screenMirror = Ioc.Default.GetRequiredService<IScreenMirrorService>();
        screenMirror.LaunchAppByPackage(package);
    }

    public static async Task InitializeApplicationAsync(AppActivationArguments activatedEventArgs)
    {
        try
        {
            switch (activatedEventArgs.Data)
            {
                case ProtocolActivatedEventArgs protocolArgs:
                    HandleProtocolActivationArgs(protocolArgs);
                    break;
                case ShareTargetActivatedEventArgs shareArgs:
                    ShowMainWindow();
                    await HandleShareTargetActivation(shareArgs);
                    break;
                default:
                    ShowMainWindow();
                    break;
            }
        }
        catch (COMException)
        {
            // Data not available 
            // Can happen when share data operation is not completed
            return;
        }
    }

    // ─── Win32 P/Invoke ────────────────────────────────────────────────────────

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private static WndProcDelegate? wndProcDelegate;
    private static IntPtr oldWndProc = IntPtr.Zero;
    private const uint WM_CLOSE = 0x0010;
    private const int GWLP_WNDPROC = -4;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int SW_HIDE = 0;
    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    // ─── Dynamic HWND Detection via EnumWindows ─────────────────────────────────

    /// <summary>
    /// Dynamically scans all Windows owned by the current process and returns
    /// the HWND of the main FlowLink window. Called fresh on each use — no caching.
    /// This is the most reliable approach for Uno Platform Skia Desktop apps.
    /// </summary>
    public static IntPtr GetMainWindowHandle()
    {
        if (!OperatingSystem.IsWindows()) return IntPtr.Zero;

        try
        {
            var procHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (procHandle != IntPtr.Zero)
            {
                return procHandle;
            }
        }
        catch { }

        uint currentPid = (uint)Environment.ProcessId;
        IntPtr foundHwnd = IntPtr.Zero;
        var debugLogs = new System.Collections.Generic.List<string>();

        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != currentPid) return true; // keep enumerating

            var title = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);

            var className = new System.Text.StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);

            var isVisible = IsWindowVisible(hWnd);

            debugLogs.Add($"HWND: {hWnd}, Title: '{title}', Class: '{className}', Visible: {isVisible}");

            // Match our main window: typically it is a visible window with a title, or a specific class name
            if (isVisible && (title.ToString().Contains("FlowLink", StringComparison.OrdinalIgnoreCase) || className.ToString().Contains("Uno", StringComparison.OrdinalIgnoreCase) || className.ToString().Contains("WPF", StringComparison.OrdinalIgnoreCase)))
            {
                foundHwnd = hWnd;
                // Don't break yet, we want to log all windows
            }
            return true;
        }, IntPtr.Zero);

        return foundHwnd;
    }

    /// <summary>
    /// Hides the main FlowLink window using native Win32 ShowWindow.
    /// This is safe even when the window is not focused.
    /// </summary>
    public static void HideMainWindow()
    {
        var hWnd = GetMainWindowHandle();
        if (hWnd != IntPtr.Zero)
            ShowWindow(hWnd, SW_HIDE);
        else
            MainWindow?.AppWindow?.Hide(); // fallback
    }

    /// <summary>
    /// Shows and brings the main FlowLink window to the foreground.
    /// Restores from minimized state if needed.
    /// </summary>
    public static void ShowMainWindow()
    {
        if (MainWindow == null) return;

        MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            var hWnd = GetMainWindowHandle();
            if (hWnd != IntPtr.Zero)
            {
                // Restore from minimized if needed
                if (IsIconic(hWnd))
                    ShowWindow(hWnd, SW_RESTORE);
                else
                    ShowWindow(hWnd, SW_SHOW);

                SetForegroundWindow(hWnd);
            }
            else
            {
                // Fallback for Uno
                MainWindow.AppWindow?.Show();
                MainWindow.Activate();
            }
        });
    }

    // ─── Window Events ─────────────────────────────────────────────────────────

    private void HookEventsForWindow()
    {
        MainWindow.Activated += Window_Activated;
        MainWindow.Closed += Window_Closed;

        try
        {
            if (MainWindow.AppWindow != null)
            {
                MainWindow.AppWindow.Closing += AppWindow_Closing;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Failed to hook AppWindow.Closing: {ex.Message}");
        }

        if (OperatingSystem.IsWindows())
        {
            MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(500);
                var hWnd = GetMainWindowHandle();
                if (hWnd != IntPtr.Zero)
                {
                    SetupWin32CloseHook(hWnd);
                }
            });
        }
    }

    private static void SetupWin32CloseHook(IntPtr hWnd)
    {
        try
        {
            if (oldWndProc != IntPtr.Zero) return;

            wndProcDelegate = (h, msg, wp, lp) =>
            {
                if (msg == WM_CLOSE && HandleClosedEvents)
                {
                    HideMainWindow();
                    ShowFirstTimeTrayNotification();
                    return IntPtr.Zero;
                }
                return CallWindowProc(oldWndProc, h, msg, wp, lp);
            };

            var newWndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);
            oldWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, newWndProcPtr);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] SetupWin32CloseHook error: {ex.Message}");
        }
    }

    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (HandleClosedEvents)
        {
            args.Cancel = true; // Prevent actual closure/destruction on Skia/WinUI

            // Hide the window — services stay alive
            HideMainWindow();

            // Show one-time tray notification
            ShowFirstTimeTrayNotification();
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        if (HandleClosedEvents)
        {
            args.Handled = true;

            // Hide the window — services stay alive
            HideMainWindow();

            // Show one-time tray notification
            ShowFirstTimeTrayNotification();
        }
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        var stateStr = args.WindowActivationState.ToString();
        if (stateStr == "CodeActivated" || stateStr == "PointerActivated")
            return;

        ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Environment.ProcessId;
    }

    // ─── First-time Tray Notification ──────────────────────────────────────────

    private const string TrayNotificationShownKey = "TrayNotificationShown";

    private static void ShowFirstTimeTrayNotification()
    {
#if WINDOWS
        try
        {
            bool alreadyShown = ApplicationData.Current.LocalSettings.Values[TrayNotificationShownKey] is bool b && b;
            if (alreadyShown) return;

            ApplicationData.Current.LocalSettings.Values[TrayNotificationShownKey] = true;

            var builder = new Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder()
                .AddText("TrayNotification.Title".GetLocalizedResource())
                .AddText("TrayNotification.Message".GetLocalizedResource());

            var notification = builder.BuildNotification();
            notification.ExpiresOnReboot = false;
            Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // Notification system may not be available; ignore silently
        }
#endif
    }

    // ─── Share Target ───────────────────────────────────────────────────────────

    public static async Task HandleShareTargetActivation(ShareTargetActivatedEventArgs args)
    {
        var shareOperation = args.ShareOperation;
        var fileTransferService = Ioc.Default.GetRequiredService<IFileTransferService>();
        var items = await shareOperation.Data.GetStorageItemsAsync();
        shareOperation.ReportDataRetrieved();
        shareOperation.ReportCompleted();
        fileTransferService.SendFilesWithPicker(items);
    }
#endif

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        => new Exception("Failed to load Page " + e.SourcePageType.FullName);

    /// <summary>
    /// Opens DeviceSettingsWindow for the specified device.
    /// </summary>
    public static DeviceSettingsWindow OpenDeviceSettingsWindow(PairedDevice device)
    {
        if (DeviceSettingsWindows.TryGetValue(device.Id, out var existingWindow))
        {
            // Window exists, activate it
            existingWindow.Activate();
            return existingWindow;
        }

        // Create new window
        var newWindow = new DeviceSettingsWindow(device);
        DeviceSettingsWindows[device.Id] = newWindow;
        newWindow.Activate();
        return newWindow;
    }

    /// <summary>
    /// Removes DeviceSettingsWindow when it is closed.
    /// </summary>
    public static void RemoveDeviceSettingsWindow(string deviceId)
    {
        DeviceSettingsWindows.Remove(deviceId);
    }
}
