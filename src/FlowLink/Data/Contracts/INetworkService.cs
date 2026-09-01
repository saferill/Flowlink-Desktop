namespace FlowLink.Data.Contracts;

public interface INetworkService
{
    Task StartServerAsync();

    /// <summary>Cancels all active auto-reconnect background tasks. Call before app exit.</summary>
    void StopAllReconnects();
}
