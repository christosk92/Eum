using System.ComponentModel.DataAnnotations;

namespace Eum.Core.Models;

public sealed class PaginatedWrapper<T>
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