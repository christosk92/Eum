namespace Eum.Cores.Spotify.Contracts;

public interface ITcpConnectionFactory
{
    ITcpConnection GetTcpConnection(
        string host,
        ushort port,
        CancellationToken ct = default);
}