using System.Net.WebSockets;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Contracts.Services;
using Microsoft.Extensions.Options;
using Websocket.Client;

namespace Eum.Cores.Spotify.Connect.Factories;

public sealed class SpotifyRemoteConnectionFactory : ISpotifyRemoteConnectionFactory
{
    private readonly IOptions<SpotifyConfig> _remoteConnectionConfig;
    private readonly ISpotifyBearerService _spotifyBearerService;
    private readonly ISpotifyClientsProvider _spClientsProvider;
    public SpotifyRemoteConnectionFactory(ISpotifyBearerService spotifyBearerService, ISpotifyClientsProvider spClientProvider, 
        IOptions<SpotifyConfig> remoteConnectionConfig)
    {
        _spotifyBearerService = spotifyBearerService;
        _spClientsProvider = spClientProvider;
        _remoteConnectionConfig = remoteConnectionConfig;
    }
    public ISpotifyRemoteConnection? GetConnection(string websocketUrl)
    {
        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
        {
            Options =
            {
                KeepAliveInterval = TimeSpan.FromDays(1),
            },
        });
        var newwsClient = new WebsocketClient(new Uri(websocketUrl), factory);
        newwsClient.ReconnectTimeout = null;
        newwsClient.IsReconnectionEnabled = false;
        return new SpotifyRemoteConnection(newwsClient, _spotifyBearerService, _spClientsProvider, 
            _remoteConnectionConfig);
    }
}