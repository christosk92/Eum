using CPlayerLib;
using Eum.Cores.Spotify.Connection;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Models;
using Microsoft.Extensions.Options;

namespace Eum.Cores.Spotify.Factories;

public sealed class SpotifyConnectionFactory : ISpotifyConnectionFactory
{
    private readonly ITcpConnectionFactory _tcpConnectionFactory;
    private readonly IApResolver _apResolver;
    private readonly IOptions<SpotifyConfig> _config;
    public SpotifyConnectionFactory(IApResolver apResolver, ITcpConnectionFactory tcpConnectionFactory, IOptions<SpotifyConfig> config)
    {
        _apResolver = apResolver;
        _tcpConnectionFactory = tcpConnectionFactory;
        _config = config;
    }
    public ISpotifyConnection GetNewConnection(LoginCredentials loginCredentials)
        => new SpotifyConnection(_apResolver, loginCredentials, _tcpConnectionFactory, _config);
}