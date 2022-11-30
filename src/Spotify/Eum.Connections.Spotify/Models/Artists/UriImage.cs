using System.Text.Json.Serialization;

namespace Eum.Connections.Spotify.Models.Artists;

public readonly struct UriImage
{
    [JsonPropertyName("uri")] public string Uri { get; init; }
}