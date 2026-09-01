using FlowLink.ViewModels;

namespace FlowLink.Services;

public interface ICallManager
{
    Task Initialize();

    CallSessionViewModel? PrimaryCall { get; }

    CallSessionViewModel? SecondaryCall { get; }

    event EventHandler? ActiveCallChanged;

    Task SwapCallsAsync();
}
