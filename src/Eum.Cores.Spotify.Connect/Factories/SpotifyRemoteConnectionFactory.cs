using Eum.Cores.Spotify.Contracts.Connect;
using Websocket.Client;

namespace Eum.Cores.Spotify.Connect.Factories;

public sealed class SpotifyRemoteConnectionFactory : ISpotifyRemoteConnectionFactory
{
    public ISpotifyRemoteConnection GetConnection(string websocketUrl)
    {
        var newwsClient = new WebsocketClient(new Uri(websocketUrl));
        newwsClient.ReconnectTimeout = null;
        return new SpotifyRemoteConnection(newwsClient);
    }
}