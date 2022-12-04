using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Eum.Artwork;
using Eum.Connections.Spotify.Attributes;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;
using Eum.Users;
using Refit;

namespace Eum.Connections.Spotify.Clients.Contracts;

[OpenUrlEndpoint]
public interface ISpotifyUsersClient
{
    [Get("/me")]
    Task<SpotifyPrivateUser> GetCurrentUser(CancellationToken ct = default);

    [Get("/users/{userId}")]
    Task<SpotifyPublicUser> GetUserOnId(string userId, CancellationToken ct = default);
}

public class SpotifyPublicUser : IUser
{
    /// <summary>
    /// The name displayed on the user's profile. null if not available.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? Name { get; init; } = null;


    [JsonConverter(typeof(FollowersWrapperToFollowersConverter))]
    public int Followers { get; init; } = 0;

    // <inheritdoc/>
    [JsonPropertyName("images")]
    [JsonConverter(typeof(SpotifyUrlImageToArtworkConverter))]
    public IArtwork[] Avatar { get; init; } = Array.Empty<IArtwork>();
    // <inheritdoc/>
    public string Id { get; init; } = null!;

    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    public SpotifyId Uri { get; init; }
}