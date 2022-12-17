using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Artists;

public class MercuryArtistPlaylist
{
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    [JsonPropertyName("uri")]
    public SpotifyId Uri { get; init; }
    [JsonPropertyName("name")]
    public string Name { get; init; }
    [JsonPropertyName("cover")]
    public UriImage Cover { get; init; }
    [JsonPropertyName("follower_count")]
    public int FollowerCount { get; init; }
}