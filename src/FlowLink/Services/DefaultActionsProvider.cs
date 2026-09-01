using FlowLink.Data.Models.Actions;
using FlowLink.Platforms.Desktop;

namespace FlowLink.Services;

public static class DefaultActionsProvider
{
    public static IEnumerable<BaseAction> GetDefaultActions()
    {
        return DesktopDefaultActions.GetDefaultActions();
    }
} 
