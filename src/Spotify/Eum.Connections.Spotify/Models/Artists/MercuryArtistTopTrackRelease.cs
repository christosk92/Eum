using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Artists;

public readonly struct MercuryArtistTopTrackRelease
{
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    [JsonPropertyName("uri")]
    public SpotifyId Uri { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; }
    
    public UriImage Cover { get; init; }
}