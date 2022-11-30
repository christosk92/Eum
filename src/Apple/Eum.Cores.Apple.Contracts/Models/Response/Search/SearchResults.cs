using Eum.Cores.Apple.Contracts.Models.Response.CoreImplementations;

namespace Eum.Cores.Apple.Contracts.Models.Response.Search;
/// <summary>
/// An object that represents the results of a catalog search query.
/// </summary>
public sealed class SearchResults
{
    // public PaginatedResourceCollectionResponse<Albums>? Albums
    // {
    //     get;
    //     init;
    // }

    public PaginatedResourceCollectionResponse<AppleMusicArtist>? Artists
    {
        get;
        init;
    }
}
