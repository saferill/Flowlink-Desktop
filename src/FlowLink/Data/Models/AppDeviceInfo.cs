using FlowLink.Data.Enums;

namespace FlowLink.Data.Models;

public partial class AppDeviceInfo(string deviceId, NotificationFilter filter) : ObservableObject
{
    private string deviceId = deviceId;
    public string DeviceId
    {
        get => deviceId;
        set => SetProperty(ref deviceId, value);
    }
    
    private bool pinned = false;
    public bool Pinned
    {
        get => pinned;
        set => SetProperty(ref pinned, value);
    }

    private NotificationFilter filter = filter;
    public NotificationFilter Filter
    {
        get => filter;
        set => SetProperty(ref filter, value);
    }
}
