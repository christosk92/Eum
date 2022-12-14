using Eum.Artists;
using Eum.Core.Contracts.Models;

namespace Eum.Models;

public sealed class CoreSearchedResponse : ICoreResponse
{
    public PaginatedWrapper<IArtist>? Artists { get; set; }
    public bool IsError => false;
}