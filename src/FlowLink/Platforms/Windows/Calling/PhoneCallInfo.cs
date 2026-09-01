using WinRTPhoneCallInfo = Windows.ApplicationModel.Calls.PhoneCallInfo;

namespace FlowLink.Platforms.Windows.Calling;

internal sealed class PhoneCallInfo(WinRTPhoneCallInfo info) : IPhoneCallInfo
{
    public string DisplayName => info.DisplayName;
    public string PhoneNumber => info.PhoneNumber;
}
