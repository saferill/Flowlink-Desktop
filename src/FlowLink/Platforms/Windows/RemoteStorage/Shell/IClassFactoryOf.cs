using static Vanara.PInvoke.Ole32;

namespace FlowLink.Platforms.Windows.RemoteStorage.Shell;

public interface IClassFactoryOf : IClassFactory
{
    Type Type { get; }
}
