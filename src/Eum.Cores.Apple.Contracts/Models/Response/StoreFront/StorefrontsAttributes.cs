using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Eum.Cores.Apple.Contracts.Helper;

namespace Eum.Cores.Apple.Contracts.Models.Response.StoreFront;
/// <summary>
/// The attributes for the storefronts resource.
/// </summary>
public sealed class StorefrontsAttributes
{
    /// <summary>
    /// (Required) The default supported RFC4646 language tag for the storefront.
    /// </summary>
    [Required]
    public string DefaultLanguageTag
    {
        get;
        init;
    } = null!;

    /// <summary>
    /// (Required) Attribute indicating the level that this storefront can display explicit content
    /// </summary>
    [JsonConverter(typeof(ExplicitContentPolicyTypeConverter))]
    public ExplicitContentPolicyType ExplicitContentPolicy
    {
        get;
        init;
    }
    /// <summary>
    /// (Required) The localized name of the storefront.
    /// </summary>
    [Required]
    public string Name
    {
        get;
        init;
    } = null!;

    /// <summary>
    /// (Required) The supported RFC4646 language tags for the storefront.
    /// </summary>
    [Required]
    public string[] SupportedLanguageTags
    {
        get;
        init;
    } = null!;
}
