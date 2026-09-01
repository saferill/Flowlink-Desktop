using FlowLink.Data.Models;

namespace FlowLink.Data.Contracts;

public interface ISftpService
{
    Task InitializeAsync(PairedDevice device, SftpServerInfo info);

    void Remove(string deviceId);

    void RemoveAll();
}
