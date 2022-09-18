using CPlayerLib;
using Eum.Cores.Spotify.Connection;
using Eum.Cores.Spotify.Contracts;

namespace Eum.Cores.Spotify.Factories;

internal sealed class SpotifyConnectionFactory : ISpotifyConnectionFactory
{
    private readonly ITcpConnectionFactory _tcpConnectionFactory;
    private readonly IApResolver _apResolver;
    public SpotifyConnectionFactory(IApResolver apResolver, ITcpConnectionFactory tcpConnectionFactory)
    {
        _apResolver = apResolver;
        _tcpConnectionFactory = tcpConnectionFactory;
    }
    public ISpotifyConnection GetNewConnection(LoginCredentials loginCredentials)
        => new SpotifyConnection(_apResolver, loginCredentials, _tcpConnectionFactory);
}