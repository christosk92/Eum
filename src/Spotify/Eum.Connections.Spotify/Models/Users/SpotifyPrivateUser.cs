using System.Text.Json.Serialization;
using Eum.Artwork;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Users;

namespace Eum.Connections.Spotify.Models.Users;

public class SpotifyPrivateUser : IUser
{
    /// <summary>
    /// The country of the user, as set in the user's account profile. An ISO 3166-1 alpha-2 country code.
    /// </summary>
    public string Country { get; init; } = null!;

    /// <summary>
    /// The name displayed on the user's profile. null if not available.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? Name { get; init; } = null;

    /// <summary>
    /// The user's email address, as entered by the user when creating their account. Important! This email address is unverified; there is no proof that it actually belongs to the user. 
    /// </summary>
    public string Email { get; init; } = null!;

    /// <summary>
    /// The user's explicit content settings.
    /// </summary>
    public ExplicitContentObject ExplicitContent { get; init; } = new ExplicitContentObject();

    [JsonConverter(typeof(FollowersWrapperToFollowersConverter))]
    public int Followers { get; init; } = 0;

    /// <inheritdoc/>
    [JsonPropertyName("images")]
    [JsonConverter(typeof(SpotifyUrlImageToArtworkConverter))]
    public IArtwork[] Avatar { get; init; } = Array.Empty<IArtwork>();

    /// <inheritdoc/>
    public string Id { get; init; } = null!;

    /// <summary>
    /// The user's Spotify subscription level: "premium", "free", etc. (The subscription level "open" can be considered the same as "free".)
    /// </summary>
    public string Product { get; init; } = null!;
    
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    public SpotifyId Uri { get; init; }
}

public readonly record struct ExplicitContentObject
{
    /// <summary>
    /// When true, indicates that explicit content should not be played.
    /// </summary>
    [JsonPropertyName("filter_enabled")]
    public bool FilterEnabled { get; init; }
    
    /// <summary>
    /// When true, indicates that the explicit content setting is locked and can't be changed by the user.
    /// </summary>
    [JsonPropertyName("filter_locked")]
    public bool FilterLocked { get; init; }
}