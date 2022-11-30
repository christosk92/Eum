using Eum.Cores.Apple.Contracts.Models.Response;
using Eum.Cores.Apple.Contracts.Models.Response.CoreImplementations;
using Refit;

namespace Eum.Cores.Apple.Contracts.Clients;

public interface IArtists
{
    /// <summary>
    /// Fetch an artist by using the artist’s identifier.
    /// </summary>
    /// <param name="id">(Required) The unique identifier for the artist.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="StoreFrontNotConfiguredException">Make sure the storefront is configured by calling <see cref="IAppleCore.StoreFrontProvider"/></exception>
    [Get("/catalog/storefront_id/artists/{id}")]
    Task<PaginatedResourceCollectionResponse<AppleMusicArtist>> GetArtistAsync(string id, CancellationToken ct = default);
}