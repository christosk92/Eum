namespace Eum.Cores.Spotify.Contracts.CoreConnection;

public interface ITcpConnectionFactory
{
    ITcpConnection GetTcpConnection(
        string host,
        ushort port,
        CancellationToken ct = default);
}