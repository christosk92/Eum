using Eum.Core.Models;

namespace Eum.Core.Contracts;

/// <summary>
/// A generic contract for a generic music service, such as AppleMusic or Spotify.
/// </summary>
public interface IMusicCore
{
    CoreType Type { get; }
    bool IsAuthenticated { get; }
    
    /// <summary>
    /// Performs authentication on the external server, if not already done,
    /// and returns a boolean indicating whether or not the authentication was successful.
    /// </summary>
    /// <param name="ct">A cancellation token to cancel the ongoing task.</param>
    /// <returns>A value indicating the result of authentication.</returns>
    ValueTask<bool> AuthenticateAsync(CancellationToken ct = default);

    
    /// <summary>
    /// Get an artist based on it's id on the external service.
    /// </summary>
    /// <param name="id">The id of the artist to lookup.</param>
    /// <param name="ct">A cancellation token to cancel the ongoing task.</param>
    /// <returns>The artist if found successfully.</returns>
    Task<IArtist> GetArtistAsync(string id, CancellationToken ct = default);

    Task<CoreSearchedResponse> SearchAsync(string query, 
        CancellationToken ct = default);
    
}