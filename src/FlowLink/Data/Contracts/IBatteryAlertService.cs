using FlowLink.Data.Models;

namespace FlowLink.Data.Contracts;

public interface IBatteryAlertService
{
    Task HandleBatteryStateAsync(PairedDevice device, BatteryState batteryState);
}
