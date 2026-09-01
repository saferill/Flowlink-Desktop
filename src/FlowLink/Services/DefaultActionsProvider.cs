using FlowLink.Data.Models.Actions;
using FlowLink.Platforms.Windows;
using FlowLink.Platforms.Desktop;

namespace FlowLink.Services;

public static class DefaultActionsProvider
{
    public static IEnumerable<BaseAction> GetDefaultActions()
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsDefaultActions.GetDefaultActions();
        }
        else
        {
            return DesktopDefaultActions.GetDefaultActions();
        }
    }
} 
