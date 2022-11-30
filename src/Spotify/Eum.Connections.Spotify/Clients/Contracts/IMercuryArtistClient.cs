using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Models.Artists;

namespace Eum.Connections.Spotify.Clients.Contracts;

public interface IMercuryArtistClient
{
    Task<MercuryArtist> GetArtistOverview(string artistId, string locale,CancellationToken ct = default);
}