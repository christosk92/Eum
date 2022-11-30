using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Attributes;
using Eum.Connections.Spotify.Models.Users;
using Refit;

namespace Eum.Connections.Spotify.Clients.Contracts;

[OpenUrlEndpoint]
public interface ISpotifyUsersClient
{
    [Get("/me")]
    Task<SpotifyPrivateUser> GetCurrentUser(CancellationToken ct = default);
}