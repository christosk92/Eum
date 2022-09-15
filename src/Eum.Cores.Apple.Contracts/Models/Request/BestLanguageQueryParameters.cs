using System.ComponentModel.DataAnnotations;
using Refit;

namespace Eum.Cores.Apple.Contracts.Models.Request;
public sealed class BestLanguageQueryParameters
{
    /// <summary>
    /// (Required) A list of languages to accept.
    /// </summary>
    [AliasAs("acceptLanguage")]
    [Query(",")]
    [Required]
    public string AcceptLanguage
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

}
