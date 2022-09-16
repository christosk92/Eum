using CPlayerLib;

namespace Eum.Cores.Spotify.Contracts;

public interface ISpotifyConnection : IDisposable
{
    Guid ConnectionId { get; }
    bool IsAlive { get; }
    bool IsAuthenticated { get; }
    
    /// <summary>
    ///     Opens a TCP connection to spotify and connects but does not authenticate.
    /// </summary>
    /// <param name="ct">Cancellation token for the asynchronous task.</param>
    /// <returns></returns>
    /// <exception cref="IOException">
    ///     Thrown when an issue occurs with the underlying socket and may not be Spotify's issue.
    /// </exception>
    /// <exception cref="AccessViolationException">
    ///     Thrown when a handshake could not be verified. This can be due to a compromised network.
    /// </exception>
    /// <exception cref="SpotifyConnectionException">
    ///     Thrown when bad data is returned from Spotify.
    ///     This usually means something went wrong in the connection and a new one has to be established.
    Task InstantiateConnectionAsync(CancellationToken ct = default);

    Task<APWelcome> AuthenticateAsync(LoginCredentials loginCredentials, CancellationToken ct = default);
}