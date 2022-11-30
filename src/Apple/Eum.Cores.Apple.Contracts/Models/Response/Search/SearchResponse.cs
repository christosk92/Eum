using System.ComponentModel.DataAnnotations;

namespace Eum.Cores.Apple.Contracts.Models.Response.Search;
/// <summary>
/// The response to a search request.
/// </summary>
public sealed class SearchResponse
{
    /// <summary>
    /// (Required) The results included in the response to a search request.
    /// </summary>
    [Required]
    public SearchResults Results
    {
        get;
        init;
    } = null!;
}
