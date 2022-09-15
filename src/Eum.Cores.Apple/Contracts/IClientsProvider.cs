using Eum.Core.Contracts;
using Eum.Cores.Apple.Contracts.Clients;

namespace Eum.Cores.Apple.Contracts;

public interface IClientsProvider
{
    IArtists ArtistClient { get; }
    IStoreFronts StoreFronts { get; }
    ISearch SearchClient { get; }
}