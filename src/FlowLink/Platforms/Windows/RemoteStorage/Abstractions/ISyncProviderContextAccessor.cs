using FlowLink.Platforms.Windows.Abstractions;

namespace FlowLink.Platforms.Windows.RemoteStorage.Abstractions;

public interface ISyncProviderContextAccessor
{
    SyncProviderContext Context { get; }
}
