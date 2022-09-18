
namespace Eum.Cores.Spotify.Contracts;

public interface ISpotifyConnection : IDisposable
{
    Guid ConnectionId { get; }
    
    /// <summary>
    /// Initiate a client hello (handshake) with the server.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>The written data to the server, as a byte array. Use this to verify the signature.</returns>
    Task 
        HandshakeAsync(CancellationToken ct = default);
}