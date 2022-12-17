using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;
using Eum.Enums;

namespace Eum.Connections.Spotify.Models.Artists;

public class PinnedItem
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("TYPE")]
    public EntityType Type { get; init; }

    [JsonPropertyName("uri")]
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    public SpotifyId Uri { get; init; }

    [JsonPropertyName("title")] public string Title { get; init; }
    [JsonPropertyName("image")] public string Image { get; init; }
    [JsonPropertyName("subtitle")] public string Subtitle { get; init; }
    [JsonPropertyName("comment")] public string Comment { get; init; }

    [JsonPropertyName("secondsToExpiration")]
    public ulong SecondsToExpiration { get; init; }

    [JsonPropertyName("backgroundImage")] public string BackgroundImage { get; init; }
}