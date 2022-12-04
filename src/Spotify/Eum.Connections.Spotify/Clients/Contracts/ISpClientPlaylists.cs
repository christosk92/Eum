using Eum.Connections.Spotify.Attributes;
using Eum.Spotify.playlist4;
using GuerrillaNtp;
using Refit;
using System.Net.Http;

namespace Eum.Connections.Spotify.Clients.Contracts
{
    [SpClientEndpoint]
    public interface ISpClientPlaylists
    {
        //playlist/v2/user/5fc6rpj232xgrtzzw4q3n790j/rootlist?decorate=revision%2Clength%2Cattributes%2Ctimestamp%2Cowner&bustCache=1670092611526
        [Get("/playlist/v2/user/{userId}/rootlist")]
        Task<HttpResponseMessage> GetPlaylists(string userId, 
            GetPlaylistsRequest request, CancellationToken ct = default);
        [Get("/playlist/v2/playlist/{playlistId}")]
        Task<HttpResponseMessage> GetPlaylist(string playlistId,CancellationToken ct = default);
    }

    public class GetPlaylistsRequest
    {
        [AliasAs("decorate")]
        public string Decorate { get; init; } = "revision,length,attributes,timestamp,owner";
    }
}
