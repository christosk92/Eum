using System.Collections.Generic;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Artists.Discography;

public class DiscographyTrackRelease
{
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    public SpotifyId Uri { get; init; }

    [JsonPropertyName("playcount")] public ulong PlayCount { get; init; }

    [JsonPropertyName("name")] public string Name { get; init; }

    [JsonPropertyName("popularity")] public int Popularity { get; init; }

    [JsonPropertyName("number")] public int Number { get; init; }

    [JsonPropertyName("duration")] public int Duration { get; init; }

    [JsonPropertyName("explicit")] public bool Explicit { get; init; }

    [JsonPropertyName("playable")] public bool Playable { get; init; }

    [JsonPropertyName("artists")] public IList<DiscographyTrackArtist> Artists { get; init; }
}