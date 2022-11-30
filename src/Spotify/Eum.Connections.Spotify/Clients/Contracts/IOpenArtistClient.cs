using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Attributes;
using Eum.Connections.Spotify.Models.Artists;
using Refit;

namespace Eum.Connections.Spotify.Clients.Contracts;

[OpenUrlEndpoint]
public interface IOpenArtistClient
{
    /// <summary>
    /// Get Spotify catalog information for a single artist identified by their unique Spotify ID.
    /// </summary>
    /// <param name="id">The Spotify ID of the artist.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Get("/artists/{id}")]
    Task<SpotifyOpenArtist> GetArtistOnId(string id, CancellationToken ct = default);
}