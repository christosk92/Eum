using System.Text.Json.Serialization;

namespace Eum.Connections.Spotify.Models.Artists;

public readonly struct MercuryArtistHeader
{
    [JsonPropertyName("image")]
    public string Image { get; init; }
}