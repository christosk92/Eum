using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Artists.Discography;

public readonly struct DiscographyTrackArtist
{
    [JsonPropertyName("name")] public string Name { get; init; }

    [JsonPropertyName("uri")]
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    public SpotifyId Uri { get; init; }
}