using FlowLink.Data.Models.Actions;
#if WINDOWS
using FlowLink.Platforms.Windows;
#elif DESKTOP
using FlowLink.Platforms.Desktop;
#endif

namespace FlowLink.Services;

public static class DefaultActionsProvider
{
    public static IEnumerable<BaseAction> GetDefaultActions()
    {
#if WINDOWS
        return WindowsDefaultActions.GetDefaultActions();
#elif DESKTOP
        return DesktopDefaultActions.GetDefaultActions();
#endif
    }
} 
