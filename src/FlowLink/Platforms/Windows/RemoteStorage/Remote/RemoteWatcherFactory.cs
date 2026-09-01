using FlowLink.Platforms.Windows.RemoteStorage.Abstractions;
using FlowLink.Platforms.Windows.RemoteStorage.RemoteAbstractions;

namespace FlowLink.Platforms.Windows.RemoteStorage.Remote;

public class RemoteWatcherFactory(SyncProviderContextAccessor contextAccessor, IEnumerable<LazyRemote<IRemoteWatcher>> options)
    : RemoteFactory<IRemoteWatcher>(contextAccessor, options)
{ }
