namespace Eum.Models;

public sealed class PaginatedWrapper<T>
{
    /// <summary>
    /// A paginated collection of resources for the request.
    /// </summary>
    public IEnumerable<T> Data
    {
        get;
        set;
    } = null!;

    /// <summary>
    /// A relative cursor to fetch the next paginated collection of resources for the request if more exist.
    /// </summary>
    public string? Next
    {
        get;
        set;
    }
}