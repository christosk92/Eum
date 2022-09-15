using Refit;

namespace Eum.Cores.Apple.Contracts.Models.Request;
public sealed class PagingStoreFrontQueryParameters
{
    /// <summary>
    /// The localization to use, specified by a language tag. The possible values are in the supportedLanguageTags array belonging to the Storefront object specified by storefront. Otherwise, the default is defaultLanguageTag in Storefront.
    /// </summary>
    [AliasAs("l")]
    public string? Language
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

    /// <summary>
    /// Additional relationships to include in the fetch.
    /// </summary>
    [AliasAs("include")]
    [Query(",")]
    public string[]? Include
    {
        get;
        init;
    }

    /// <summary>
    /// A list of attribute extensions to apply to resources in the response.
    /// </summary>
    [AliasAs("extend")]
    [Query(",")]
    public string[]? Extend
    {
        get;
        init;
    }

}
