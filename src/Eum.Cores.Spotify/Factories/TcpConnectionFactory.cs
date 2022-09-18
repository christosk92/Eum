using System.Net.Sockets;
using Eum.Cores.Spotify.Connection;
using Eum.Cores.Spotify.Contracts;

namespace Eum.Cores.Spotify.Factories;

public sealed class TcpConnectionFactory: ITcpConnectionFactory
{
    public ITcpConnection GetTcpConnection(string host, ushort port, CancellationToken ct = default)
    {
        return new RawTcpConnection(new TcpClient(host, port)
        {
            ReceiveTimeout = 500
        });
    }
}