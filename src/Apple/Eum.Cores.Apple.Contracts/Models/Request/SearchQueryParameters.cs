using System.ComponentModel.DataAnnotations;
using Refit;

namespace Eum.Cores.Apple.Contracts.Models.Request;
public sealed class SearchQueryParameters
{
    /// <summary>
    /// (Required) The entered text for the search with ‘+’ characters between each word, to replace spaces (for example term=james+br).
    /// </summary>
    [Required]
    [AliasAs("term")]
    [Query("+")]
    public string Term
    {
        get;
        init;
    } = null!;

    /// <summary>
    ///The localization to use, specified by a language tag. <br/> The possible values are in the supportedLanguageTags array belonging to the Storefront object specified by storefront. Otherwise, the default is defaultLanguageTag in Storefront.
    /// </summary>
    [AliasAs("l")]
    public string? Language
    {
        get;
        init;
    }

    /// <summary> 
    /// (Required) The list of the types of resources to include in the results. <br/>
    /// Possible values: activities, albums, apple-curators, artists, curators, music-videos, playlists, record-labels, songs, stations
    /// </summary>
    [AliasAs("types")]
    public string[]? Types
    {
        get;
        init;
    }

    /// <summary>
    /// The number of objects or number of objects in the specified relationship returned.
    /// </summary>
    [AliasAs("limit")]
    public uint Limit
    {
        get;
        init;
    } = 25;

    /// <summary>
    /// The offset to use for a paginated request. See <see href="https://developer.apple.com/documentation/applemusicapi/fetching_resources_by_page">Fetching Resources by Page.</see>
    /// </summary>
    [AliasAs("offset")]
    public uint Offset
    {
        get;
        init;
    } = 0;

}
