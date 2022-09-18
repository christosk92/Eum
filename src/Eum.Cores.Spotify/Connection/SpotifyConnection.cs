using System.Diagnostics;
using CPlayerLib;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Helpers;

namespace Eum.Cores.Spotify.Connection;

public sealed class SpotifyConnection : ISpotifyConnection
{
    private readonly ITcpConnectionFactory _tcpConnectionFactory;
    private readonly LoginCredentials _loginCredentials;
    private readonly IApResolver _apResolver;

    private ITcpConnection? _currentConnection;
    public SpotifyConnection(IApResolver apResolver, LoginCredentials loginCredentials, ITcpConnectionFactory tcpConnectionFactory)
    {
        _apResolver = apResolver;
        _loginCredentials = loginCredentials;
        _tcpConnectionFactory = tcpConnectionFactory;
        ConnectionId = Guid.NewGuid();
    }
    public Guid ConnectionId { get; }
    public string DeviceId { get; private set; }

    public bool IsAlive => APWelcome != null && _currentConnection is { IsAlive: true };
    public APWelcome? APWelcome { get; private set; }

    public async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (_currentConnection is
            {
                IsAlive: true
            })
            return;

        _currentConnection?.Dispose();

        var (host, port) = await _apResolver.GetClosestAccessPoint(ct);
        _currentConnection = _tcpConnectionFactory.GetTcpConnection(host, port, ct);

        DeviceId = Utils.RandomHexString(40).ToLower();
        var sw = Stopwatch.StartNew();
        var handshake = await _currentConnection.HandshakeAsync(ct);
        APWelcome = await _currentConnection.AuthenticateAsync(_loginCredentials, DeviceId, ct);
        sw.Stop();
    }

    public void Dispose()
    {
        _currentConnection?.Dispose();
        APWelcome = null;
    }
}