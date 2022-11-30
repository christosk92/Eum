using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Artists;

public readonly struct MercuryArtistTopTrack
{
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    [JsonPropertyName("uri")]
    public SpotifyId Uri { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; }
    
    [JsonPropertyName("playcount")]
    public long Playcount { get; init; }
    
    [JsonPropertyName("release")]
    public MercuryArtistTopTrackRelease Release { get; init; }
    
    [JsonPropertyName("explicit")]
    public bool Explicit { get; init; }
}