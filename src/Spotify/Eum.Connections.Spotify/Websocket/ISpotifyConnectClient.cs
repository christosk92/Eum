using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eum.Connections.Spotify.Websocket;

public interface ISpotifyConnectClient
{
    /// <summary>
    /// A boolean flag that indicates whether we're waiting for a Connection Id. <br/>
    /// When set to true, the next "connection id" message will result in a similarevent.
    /// </summary>
    public bool WaitingForConnectionId { get; }

    /// <summary>
    /// The string connection id for the Dealer connection.
    /// </summary>
    public string? ConnectionId { get; }
    event EventHandler<string?> ConnectionIdReceived;
    /// <summary>
    /// The string connection id uri for the Dealer connection.
    /// </summary>
    public Uri? ConnectionUri { get; }

    bool Connected { get; }
    /// <summary>
    /// Authenticates the instance to the Dealer service.
    /// </summary>
    /// <param name="token">The OAuth token that identifies the current user</param>
    /// <param name="ct"></param>
    /// <returns>A task that will be resolved when the instance has been properly authenticated.</returns>
    Task<bool> Authenticate(CancellationToken ct =default);

    void Disconnect();
    
    void AddMessageListener(IMessageListener deviceStateHandler, params string[] uris);
    void AddRequestListener(IRequestListener deviceStateHandler, params string[] uris);
}
