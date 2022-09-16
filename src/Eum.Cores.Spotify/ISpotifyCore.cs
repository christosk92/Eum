using Eum.Core.Contracts;
using Eum.Core.Models;
using Eum.Cores.Spotify.Contracts;

namespace Eum.Cores.Spotify;

public class SpotifyCore : ISpotifyCore
{
    public CoreType Type => CoreType.Spotify;
    public Task<IArtist> GetArtistAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<CoreSearchedResponse> SearchAsync(string query, CancellationToken ct = default)
    {
        return Task.FromResult(new CoreSearchedResponse
        {
            Artists = new PaginatedWrapper<IArtist>
            {
                Data = new List<IArtist>()
            }
        });
    }

    public static ISpotifyCore Create(string username, string password)
    {
        return new SpotifyCore();
    }
}