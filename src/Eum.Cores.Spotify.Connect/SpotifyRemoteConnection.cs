using Eum.Cores.Spotify.Contracts.Connect;
using Websocket.Client;

namespace Eum.Cores.Spotify.Connect;

public sealed class SpotifyRemoteConnection : ISpotifyRemoteConnection
{
    private readonly WebsocketClient _wsClient;
    public SpotifyRemoteConnection(WebsocketClient websocketClient)
    {
        _wsClient = websocketClient;
        ConnectionId = Guid.NewGuid();
    }
    public Guid ConnectionId { get; }
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public bool IsAlive { get; }
    public Task<bool> EnsureConnectedAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}