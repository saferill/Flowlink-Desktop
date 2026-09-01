using System.Net;

namespace FlowLink.Services.Socket;
public interface IUdpClientProvider
{
    void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size);
}
