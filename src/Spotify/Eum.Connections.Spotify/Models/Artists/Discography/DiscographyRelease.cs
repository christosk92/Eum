using System.Collections.Generic;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Artists.Discography;

public readonly struct DiscographyRelease
{
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    public SpotifyId Uri { get; init; }

    [JsonPropertyName("name")] public string Name { get; init; }

    [JsonPropertyName("cover")] public UriImage Cover { get; init; }

    [JsonPropertyName("year")] public ushort Year { get; init; }
    [JsonPropertyName("track_count")] public ushort TrackCount { get; init; }
    [JsonPropertyName("month")] public ushort? Month { get; init; }
    [JsonPropertyName("day")] public ushort? Day { get; init; }

    [JsonConverter(typeof(DiscToListConverter))]
    [JsonPropertyName("discs")]
    public IList<IList<DiscographyTrackRelease>>? Discs { get; init; }
}