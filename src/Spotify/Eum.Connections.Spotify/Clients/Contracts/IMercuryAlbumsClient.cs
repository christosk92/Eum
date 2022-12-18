using Eum.Connections.Spotify.Models.Albums;

namespace Eum.Connections.Spotify.Clients.Contracts
{
    public interface IMercuryAlbumsClient
    {
        Task<MercuryAlbum> GetAlbum(string albumUri, string locale, string country, CancellationToken ct = default);
    }
}
