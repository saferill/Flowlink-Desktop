using System.Diagnostics;
using System.Runtime.InteropServices;
using FlowLink.Data.Contracts;
using FlowLink.Data.Models;
using FlowLink.Services;

namespace FlowLink.Platforms.Windows.Services;

public class WindowsActionService(
    IGeneralSettingsService generalSettingsService,
    ISessionManager sessionManager,
    IUserSettingsService userSettingsService,
    ILogger<WindowsActionService> logger) : BaseActionService(generalSettingsService, userSettingsService, sessionManager, logger)
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    [DllImport("PowrProf.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    private const uint WM_CLOSE = 0x0010;

    public override void HandleActionMessage(ActionInfo action)
    {
        logger.Info($"[WindowsActionService] Received action request: {action.ActionName} ({action.ActionId})");

        var actionId = action.ActionId?.ToLowerInvariant();

        switch (actionId)
        {
            case "lock":
                logger.Info("Locking Windows workstation...");
                try
                {
                    if (!LockWorkStation())
                    {
                        base.HandleActionMessage(action);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to lock workstation via API: {ex.Message}");
                    base.HandleActionMessage(action);
                }
                return;

            case "close_all":
            case "close_all_apps":
            case "closeall":
                CloseAllUserWindows();
                return;

            case "hibernate":
                logger.Info("Hibernating Windows system...");
                try
                {
                    var psi = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/h")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
                catch
                {
                    try
                    {
                        SetSuspendState(true, true, true);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to hibernate: {ex.Message}");
                        base.HandleActionMessage(action);
                    }
                }
                return;

            case "logoff":
                logger.Info("Logging off Windows user session...");
                try
                {
                    var psi = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/l")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
                catch
                {
                    try
                    {
                        ExitWindowsEx(0, 0);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to log off: {ex.Message}");
                        base.HandleActionMessage(action);
                    }
                }
                return;

            case "restart":
                logger.Info("Restarting Windows system...");
                try
                {
                    var psi = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/r /t 0 /f")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to restart: {ex.Message}");
                    base.HandleActionMessage(action);
                }
                return;

            case "shutdown":
                logger.Info("Shutting down Windows system...");
                try
                {
                    var psi = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/s /t 0 /f")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to shutdown: {ex.Message}");
                    base.HandleActionMessage(action);
                }
                return;

            default:
                base.HandleActionMessage(action);
                break;
        }
    }

    private void CloseAllUserWindows()
    {
        logger.Info("Closing all open user applications and windows...");
        uint currentPid = (uint)Process.GetCurrentProcess().Id;

        // Gracefully close processes with main windows
        try
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    if (proc.Id == (int)currentPid) continue;
                    if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(proc.MainWindowTitle))
                    {
                        proc.CloseMainWindow();
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            logger.Warn($"Error during CloseAllUserWindows processes: {ex.Message}");
        }

        // Also post WM_CLOSE to visible top-level windows excluding desktop/tray
        try
        {
            EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == currentPid || pid == 0) return true;

                    var title = new System.Text.StringBuilder(256);
                    GetWindowText(hWnd, title, 256);
                    var titleStr = title.ToString();

                    if (!string.IsNullOrWhiteSpace(titleStr) &&
                        !titleStr.Equals("Program Manager", StringComparison.OrdinalIgnoreCase) &&
                        !titleStr.Equals("Windows Input Experience", StringComparison.OrdinalIgnoreCase) &&
                        !titleStr.Equals("Taskbar", StringComparison.OrdinalIgnoreCase))
                    {
                        PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                catch { }
                return true;
            }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            logger.Warn($"EnumWindows close error: {ex.Message}");
        }
    }
}
