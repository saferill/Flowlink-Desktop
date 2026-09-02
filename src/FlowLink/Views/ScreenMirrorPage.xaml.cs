using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using FlowLink.Data.Contracts;
using FlowLink.Data.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.Foundation;

namespace FlowLink.Views;

public sealed partial class ScreenMirrorPage : Page
{
    private readonly IScreenMirrorService screenMirrorService;
    private readonly IDeviceManager deviceManager;

    // Win32 APIs for window hosting
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_CHILD = 0x40000000;
    
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public ScreenMirrorPage()
    {
        InitializeComponent();
        screenMirrorService = Ioc.Default.GetRequiredService<IScreenMirrorService>();
        deviceManager = Ioc.Default.GetRequiredService<IDeviceManager>();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        screenMirrorService.OnScrcpyWindowFound += OnScrcpyWindowFound;
        screenMirrorService.OnScrcpyWindowClosed += OnScrcpyWindowClosed;

        // If scrcpy is already running, embed it immediately
        if (screenMirrorService.CurrentScrcpyHwnd != IntPtr.Zero)
        {
            EmbedScrcpy(screenMirrorService.CurrentScrcpyHwnd);
        }
        else
        {
            UpdatePlaceholderState();
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        screenMirrorService.OnScrcpyWindowFound -= OnScrcpyWindowFound;
        screenMirrorService.OnScrcpyWindowClosed -= OnScrcpyWindowClosed;

        // If scrcpy is running, hide it so it doesn't float as a separate window when we navigate away
        if (screenMirrorService.CurrentScrcpyHwnd != IntPtr.Zero)
        {
            ShowWindow(screenMirrorService.CurrentScrcpyHwnd, SW_HIDE);
        }
    }

    private void OnScrcpyWindowFound(IntPtr hwnd)
    {
        EmbedScrcpy(hwnd);
    }

    private void OnScrcpyWindowClosed()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdatePlaceholderState();
        });
    }

    private void EmbedScrcpy(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;

        DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                // Remove window title bar and border
                int style = GetWindowLong(hwnd, GWL_STYLE);
                style &= ~WS_CAPTION;
                style &= ~WS_THICKFRAME;
                style |= WS_CHILD;
                SetWindowLong(hwnd, GWL_STYLE, style);

                // Set parent to main window HWND (dynamically retrieved via EnumWindows)
                SetParent(hwnd, App.GetMainWindowHandle());

                // Make it visible
                ShowWindow(hwnd, SW_SHOW);

                // Hide placeholder and resize
                PlaceholderPanel.Visibility = Visibility.Collapsed;
                ConnectingProgress.IsActive = false;
                ConnectingProgress.Visibility = Visibility.Collapsed;

                ResizeEmbeddedWindow(hwnd);
            }
            catch (Exception)
            {
                // Fallback to placeholder if embedding fails
                UpdatePlaceholderState();
            }
        });
    }

    private void ResizeEmbeddedWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;

        try
        {
            // Convert ContainerGrid position relative to MainWindow content area
            var transform = ContainerGrid.TransformToVisual(App.MainWindow.Content);
            Point position = transform.TransformPoint(new Point(0, 0));
            double width = ContainerGrid.ActualWidth;
            double height = ContainerGrid.ActualHeight;

            if (width > 0 && height > 0)
            {
                MoveWindow(hwnd, (int)position.X, (int)position.Y, (int)width, (int)height, true);
            }
        }
        catch (Exception)
        {
            // Ignore resize exceptions
        }
    }

    private void ContainerGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (screenMirrorService.CurrentScrcpyHwnd != IntPtr.Zero)
        {
            ResizeEmbeddedWindow(screenMirrorService.CurrentScrcpyHwnd);
        }
    }

    private void UpdatePlaceholderState()
    {
        PlaceholderPanel.Visibility = Visibility.Visible;
        PlaceholderIcon.Visibility = Visibility.Visible;
        PlaceholderText.Text = "Screen mirroring is not active.";
        ConnectingProgress.IsActive = false;
        ConnectingProgress.Visibility = Visibility.Collapsed;
        StartMirroringButton.Visibility = Visibility.Visible;
    }

    private async void StartMirroringButton_Click(object sender, RoutedEventArgs e)
    {
        var device = deviceManager.ActiveDevice;
        if (device is null)
        {
            PlaceholderText.Text = "No active device connected.";
            return;
        }

        PlaceholderIcon.Visibility = Visibility.Collapsed;
        StartMirroringButton.Visibility = Visibility.Collapsed;
        PlaceholderText.Text = "Starting screen mirror session...";
        ConnectingProgress.Visibility = Visibility.Visible;
        ConnectingProgress.IsActive = true;

        bool started = await screenMirrorService.StartScrcpy(device);
        if (!started)
        {
            UpdatePlaceholderState();
            PlaceholderText.Text = "Failed to start mirroring. Check settings and connections.";
        }
    }
}
