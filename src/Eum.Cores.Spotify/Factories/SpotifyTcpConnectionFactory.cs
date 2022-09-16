using Eum.Cores.Spotify.Connection;
using Eum.Cores.Spotify.Contracts;

namespace Eum.Cores.Spotify.Factories;

internal sealed class SpotifyTcpConnectionFactory : ISpotifyConnectionFactory
{
    private readonly IApResolver _apResolver;
    public SpotifyTcpConnectionFactory(IApResolver apResolver)
    {
        _apResolver = apResolver;
    }
    public ISpotifyConnection GetNewConnection()
        => new SpotifyTcpConnection(_apResolver);
}