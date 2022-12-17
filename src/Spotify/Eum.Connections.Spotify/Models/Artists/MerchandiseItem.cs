using System.Text.Json.Serialization;

namespace Eum.Connections.Spotify.Models.Artists;

public class MerchandiseItem
{
    [JsonPropertyName("name")] public string Name { get; init; }
    [JsonPropertyName("description")] public string Description { get; init; }
    [JsonPropertyName("link")] public string Link { get; init; }
    [JsonPropertyName("image_uri")] public string ImageUri { get; init; }
    [JsonPropertyName("price")] public string Price { get; init; }
    [JsonPropertyName("uuid")] public string Uuid { get; init; }
}