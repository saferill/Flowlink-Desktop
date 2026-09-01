using FlowLink.Data.Contracts;
using FlowLink.Data.Models;

namespace FlowLink.Platforms.Desktop.Services;

public class DesktopBatteryService : IBatteryService
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public void SendBatteryStatus(PairedDevice device)
    {
    }
}
