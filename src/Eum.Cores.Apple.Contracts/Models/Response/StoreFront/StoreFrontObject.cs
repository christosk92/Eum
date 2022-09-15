using System.ComponentModel.DataAnnotations;

namespace Eum.Cores.Apple.Contracts.Models.Response.StoreFront;
/// <summary>
/// A resource object that represents a storefront, an Apple Music and iTunes Store territory that the content is available in.
/// </summary>
public sealed class StoreFrontObject
{
    /// <summary>
    /// (Required) The identifier for the storefront.
    /// </summary>
    [Required]
    public string Id
    {
        get;
        init;
    } = null!;

    /// <summary>
    /// (Required) This value must always be storefronts.
    /// </summary>
    [Required]
    public string Type
    {
        get;
        init;
    } = null!;

    /// <summary>
    /// (Required) The relative location for the storefront resource.
    /// </summary>
    [Required]
    public string Href
    {
        get;
        init;
    } = null!;

    /// <summary>
    /// The attributes for the storefront.
    /// </summary>
    public StorefrontsAttributes? Attributes
    {
        get;
        init;
    }
}
