using FlowLink.Data.Models.Actions;

namespace FlowLink.Platforms.Desktop;

public class DesktopDefaultActions
{
    public static IReadOnlyList<BaseAction> GetDefaultActions()
    {
        if (OperatingSystem.IsWindows())
        {
            return
            [
                new ProcessAction { Id = "lock", Name = "Lock Screen", Path = "rundll32.exe", Arguments = "user32.dll,LockWorkStation" },
                new ProcessAction { Id = "close_all", Name = "Close All Apps", Path = "powershell.exe", Arguments = "-NoProfile -Command \"Get-Process | Where-Object { $_.MainWindowHandle -ne 0 -and $_.Id -ne $PID } | ForEach-Object { $_.CloseMainWindow() }\"" },
                new ProcessAction { Id = "shutdown", Name = "Shutdown", Path = "shutdown.exe", Arguments = "/s /t 0 /f" },
                new ProcessAction { Id = "restart", Name = "Restart", Path = "shutdown.exe", Arguments = "/r /t 0 /f" },
                new ProcessAction { Id = "hibernate", Name = "Hibernate", Path = "shutdown.exe", Arguments = "/h" },
                new ProcessAction { Id = "logoff", Name = "Log Off", Path = "shutdown.exe", Arguments = "/l" },
            ];
        }

        return
        [
            new ProcessAction { Id = "lock", Name = "Lock Screen", Path = "loginctl", Arguments = "lock-session" },
            new ProcessAction { Id = "hibernate", Name = "Hibernate", Path = "systemctl", Arguments = "hibernate" },
            new ProcessAction { Id = "logoff", Name = "Log Off", Path = "loginctl", Arguments = "terminate-session" },
            new ProcessAction { Id = "restart", Name = "Restart", Path = "shutdown", Arguments = "-r now" },
            new ProcessAction { Id = "shutdown", Name = "Shutdown", Path = "shutdown", Arguments = "-h now" },
        ];
    }
} 
