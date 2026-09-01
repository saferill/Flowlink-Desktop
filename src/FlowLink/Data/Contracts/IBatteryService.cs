using FlowLink.Data.Models;

namespace FlowLink.Data.Contracts;

public interface IBatteryService
{
    Task InitializeAsync();

    void SendBatteryStatus(PairedDevice device);
}
