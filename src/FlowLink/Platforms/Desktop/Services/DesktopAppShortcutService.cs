using FlowLink.Data.Contracts;
using FlowLink.Data.Models;

namespace FlowLink.Platforms.Desktop.Services;

public class DesktopAppShortcutService : IAppShortcutService
{
    public Task CreateAppShortcutAsync(ApplicationItem app)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAppShortcutAsync(string androidPackageName)
    {
        return Task.CompletedTask;
    }

    public bool IsShortcutRegistered(string androidPackageName) => false;
}
