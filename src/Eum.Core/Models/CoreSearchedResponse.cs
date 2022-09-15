using Eum.Core.Contracts;

namespace Eum.Core.Models;

public sealed class CoreSearchedResponse
{
    public PaginatedWrapper<IArtist>? Artists { get; init; }
}