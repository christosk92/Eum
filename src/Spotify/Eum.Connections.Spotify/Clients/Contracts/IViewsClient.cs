using Eum.Connections.Spotify.Attributes;
using Eum.Connections.Spotify.Models;
using Eum.Connections.Spotify.Models.Views;
using Refit;

namespace Eum.Connections.Spotify.Clients.Contracts
{
    [OpenUrlEndpoint]
    public interface IViewsClient
    {
        [Get("/v1/views/desktop-home")]
        Task<View<View<ISpotifyItem>>> GetHomeAsync(HomeRequest request,
            CancellationToken ct = default);
    }
}
