using FlowLink.Platforms.Windows.RemoteStorage.Abstractions;
using FlowLink.Platforms.Windows.RemoteStorage.RemoteAbstractions;

namespace FlowLink.Platforms.Windows.RemoteStorage.Remote;

public class RemoteReadServiceFactory(SyncProviderContextAccessor contextAccessor, IEnumerable<LazyRemote<IRemoteReadService>> options)
    : RemoteFactory<IRemoteReadService>(contextAccessor, options)
{ }
