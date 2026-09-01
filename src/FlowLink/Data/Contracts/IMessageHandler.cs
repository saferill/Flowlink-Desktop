using FlowLink.Data.Models;

namespace FlowLink.Data.Contracts;
public interface IMessageHandler
{
    void HandleMessageAsync(PairedDevice device, SocketMessage message);
}
