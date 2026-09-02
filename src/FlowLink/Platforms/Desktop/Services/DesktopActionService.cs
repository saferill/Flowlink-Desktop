using System.Diagnostics;
using System.Runtime.InteropServices;
using FlowLink.Data.Contracts;
using FlowLink.Data.Models;
using FlowLink.Data.Models.Actions;
using FlowLink.Services;

namespace FlowLink.Platforms.Desktop.Services;

public partial class DesktopActionService(
    IGeneralSettingsService generalSettingsService, 
    IUserSettingsService userSettingsService,
    ISessionManager sessionManager, 
    ILogger<DesktopActionService> logger) : BaseActionService(generalSettingsService, userSettingsService, sessionManager, logger)
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
        logger.Info($"[DesktopActionService] Received action request: {action.ActionName} ({action.ActionId})");

        var actionId = action.ActionId?.Trim().ToLowerInvariant();

        if (OperatingSystem.IsWindows())
        {
            switch (actionId)
            {
                case "lock":
                    logger.Info("Locking Windows workstation...");
                    try
                    {
                        if (LockWorkStation())
                        {
                            logger.Info("Workstation locked via LockWorkStation() successfully.");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"LockWorkStation error: {ex.Message}");
                    }

                    try
                    {
                        Process.Start(new ProcessStartInfo("rundll32.exe", "user32.dll,LockWorkStation")
                        {
                            UseShellExecute = true
                        });
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to lock via rundll32 fallback: {ex.Message}");
                    }
                    break;

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
                        return;
                    }
                    catch
                    {
                        try
                        {
                            SetSuspendState(true, true, true);
                            return;
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Failed to hibernate: {ex.Message}");
                        }
                    }
                    break;

                case "sleep":
                    logger.Info("Putting Windows system to sleep...");
                    try
                    {
                        SetSuspendState(false, true, true);
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to sleep via SetSuspendState: {ex.Message}");
                        try
                        {
                            Process.Start(new ProcessStartInfo("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0")
                            {
                                UseShellExecute = true
                            });
                            return;
                        }
                        catch { }
                    }
                    break;

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
                        return;
                    }
                    catch
                    {
                        try
                        {
                            ExitWindowsEx(0, 0);
                            return;
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Failed to log off: {ex.Message}");
                        }
                    }
                    break;

                case "restart":
                    logger.Info("Restarting Windows system...");
                    ExecuteShutdownCommand("/r /t 0 /f");
                    return;

                case "shutdown":
                    logger.Info("Shutting down Windows system...");
                    ExecuteShutdownCommand("/s /t 0 /f");
                    return;
            }
        }

        // Custom action or non-Windows fallback
        base.HandleActionMessage(action);
    }

    private void ExecuteShutdownCommand(string arguments)
    {
        var shutdownPath = System.IO.Path.Combine(Environment.SystemDirectory, "shutdown.exe");
        try
        {
            var psi = new ProcessStartInfo(shutdownPath, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
            logger.Info($"Successfully launched shutdown command: {arguments}");
            return;
        }
        catch (Exception ex)
        {
            logger.Warn($"Failed with UseShellExecute=false: {ex.Message}, retrying with UseShellExecute=true...");
        }

        try
        {
            var psi = new ProcessStartInfo("shutdown.exe", arguments)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
            logger.Info($"Successfully launched shutdown command via shell: {arguments}");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to execute shutdown command ({arguments}): {ex.Message}");
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
