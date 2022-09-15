using Eum.Connections.Apple.Models.Search;
using Eum.Cores.Apple.Models.Request;
using Refit;

namespace Eum.Cores.Apple.Contracts.Clients;
public interface ISearch
{
    /// <summary>
    /// Search the catalog by using a query.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Get("/catalog/storefront_id/search")]
    Task<SearchResponse> SearchCatalogue(SearchQueryParameters parameters,
        CancellationToken ct = default);
}
