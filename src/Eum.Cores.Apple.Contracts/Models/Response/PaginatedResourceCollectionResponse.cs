using System.ComponentModel.DataAnnotations;

namespace Eum.Cores.Apple.Contracts.Models.Response;
/// <summary>
/// A response object composed of paginated resource objects for the request.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class PaginatedResourceCollectionResponse<T>
{

    /// <summary>
    /// A paginated collection of resources for the request.
    /// </summary>
    [Required]
    public IEnumerable<T> Data
    {
        get;
        init;
    } = null!;

    /// <summary>
    /// A relative cursor to fetch the next paginated collection of resources for the request if more exist.
    /// </summary>
    public string? Next
    {
        get;
        init;
    }
}
