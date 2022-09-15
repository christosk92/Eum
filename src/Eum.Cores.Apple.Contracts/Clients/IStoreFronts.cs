using Eum.Cores.Apple.Contracts.Models.Request;
using Eum.Cores.Apple.Contracts.Models.Response;
using Eum.Cores.Apple.Contracts.Models.Response.StoreFront;
using Refit;

namespace Eum.Cores.Apple.Contracts.Clients;

public interface IStoreFronts
{
    /// <summary>
    /// Fetch a storefront for a specific user.
    /// </summary>
    /// <param name="queryParameters">Extra query parameters to include.</param>
    /// <param name="ct">A cancellation token to cancel the ongoing task.</param>
    /// <returns><see cref="PaginatedResourceCollectionResponse{T}"/></returns>
    [Get("/me/storefront")]
    Task<PaginatedResourceCollectionResponse<StoreFrontObject>> GetCurrentStoreFrontAsync(
        PagingStoreFrontQueryParameters? queryParameters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Fetch a single storefront by using its identifier.
    /// </summary>
    /// <param name="id">The identifier (an ISO 3166 alpha-2 country code) for the storefront you want to fetch.</param>
    /// <param name="queryParameters"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Get("/storefronts/{id}")]
    Task<StoreFrontObject>  GetStorefront(
        string id,
        StoreFrontQueryParameters? queryParameters = null,
        CancellationToken ct = default);


    /// <summary>
    /// Fetch one or more storefronts by using their identifiers.
    /// </summary>
    /// <param name="queryParameters"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Get("/storefronts")]
    Task<PaginatedResourceCollectionResponse<StoreFrontObject>> GetStorefronts(
        MultipleStoreFrontQueryParameters? queryParameters = null,
        CancellationToken ct = default);
}
 