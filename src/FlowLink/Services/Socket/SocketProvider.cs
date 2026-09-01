using NetCoreServer;
using System.Net;
using System.Net.Sockets;
using UdpClient = NetCoreServer.UdpClient;

namespace FlowLink.Services.Socket;

public partial class ServerSession : SslSession
{
    private readonly ITcpServerProvider socketProvider;

    public ServerSession(SslServer server, ITcpServerProvider socketProvider) : base(server)
    {
        this.socketProvider = socketProvider;
        OptionNoDelay = true;
        OptionReceiveBufferSize = 2 * 1024 * 1024;
        OptionSendBufferSize = 2 * 1024 * 1024;
        OptionKeepAlive = true;
    }

    protected override void OnDisconnected()
    {
        socketProvider.OnDisconnected(this);
    }

    protected override void OnConnected()
    {
        socketProvider.OnConnected(this);
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        socketProvider.OnReceived(this, buffer, offset, size);
    }

    protected override void OnError(SocketError error)
    {
        socketProvider.OnError(error);
    }
}

public partial class Server : SslServer
{
    private readonly ITcpServerProvider socketProvider;

    public Server(SslContext context, IPAddress address, int port, ITcpServerProvider socketProvider) : base(context, address, port)
    {
        this.socketProvider = socketProvider;
        OptionNoDelay = true;
        OptionReuseAddress = true;
        OptionReceiveBufferSize = 2 * 1024 * 1024;
        OptionSendBufferSize = 2 * 1024 * 1024;
        OptionKeepAlive = true;
    }

    protected override SslSession CreateSession()
    {
        return new ServerSession(this, socketProvider);
    }

    protected override void OnError(SocketError error)
    {
        socketProvider.OnError(error);
    }
}

public partial class Client : SslClient
{
    private readonly ITcpClientProvider socketProvider;

    public Client(SslContext context, string address, int port, ITcpClientProvider socketProvider) : base(context, address, port)
    {
        this.socketProvider = socketProvider;
        OptionNoDelay = true;
        OptionReceiveBufferSize = 2 * 1024 * 1024;
        OptionSendBufferSize = 2 * 1024 * 1024;
        OptionKeepAlive = true;
    }

    protected override void OnConnected()
    {
        socketProvider.OnConnected(this);
    }

    protected override void OnDisconnected()
    {
        socketProvider.OnDisconnected(this);
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        socketProvider.OnReceived(this, buffer, offset, size);
    }

    protected override void OnError(SocketError error)
    {
        socketProvider.OnError(this, error);
    }

    protected override void OnHandshaked()
    {
        socketProvider.OnHandshaked(this);
    }
}


public partial class MulticastClient(string address, int port, IUdpClientProvider socketProvider, ILogger logger) : UdpClient(address, port)
{

    protected override void OnConnected()
    {
        ReceiveAsync();
    }

    protected override void OnDisconnected()
    {
    }

    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        socketProvider.OnReceived(endpoint, buffer, offset, size);
        ReceiveAsync();
    }
    protected override void OnError(SocketError error)
    {
        logger.Error($"Session {Id} encountered error: {error}");
    }
}
