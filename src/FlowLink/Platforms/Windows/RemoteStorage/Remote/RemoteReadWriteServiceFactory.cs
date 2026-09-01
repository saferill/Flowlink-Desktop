using FlowLink.Platforms.Windows.RemoteStorage.Abstractions;
using FlowLink.Platforms.Windows.RemoteStorage.RemoteAbstractions;

namespace FlowLink.Platforms.Windows.RemoteStorage.Remote;

public class RemoteReadWriteServiceFactory(SyncProviderContextAccessor contextAccessor, IEnumerable<LazyRemote<IRemoteReadWriteService>> options)
    : RemoteFactory<IRemoteReadWriteService>(contextAccessor, options)
{ }
