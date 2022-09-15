using Refit;

namespace Eum.Cores.Apple.Contracts.Models.Request;
public sealed class StoreFrontQueryParameters
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
    /// Additional relationships to include in the fetch.
    /// </summary>
    [AliasAs("include")]
    public string[]? Include
    {
        get;
        init;
    }

    /// <summary>
    /// A list of attribute extensions to apply to resources in the response.
    /// </summary>
    [AliasAs("extend")]
    public string[]? Extend
    {
        get;
        init;
    }

}
