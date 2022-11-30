using Refit;

namespace Eum.Cores.Apple.Contracts.Models.Request;
public sealed class MultipleStoreFrontQueryParameters
{
    /// <summary>
    /// (Required) A list of the identifiers (ISO 3166 alpha-2 country codes) for the storefronts you want to fetch.
    /// </summary>
    [AliasAs("ids")]
    [Query(",")]
    public string[] Ids
    {
        get;
        init;
    }

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
