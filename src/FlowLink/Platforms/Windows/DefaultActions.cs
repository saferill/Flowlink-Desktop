using FlowLink.Data.Models.Actions;

namespace FlowLink.Platforms.Windows;

public class WindowsDefaultActions
{
    public static IReadOnlyList<BaseAction> GetDefaultActions()
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
} 
