using Eum.Cores.Apple.Models.CoreImplementations;

namespace Eum.Cores.Apple.Models.Search;
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
