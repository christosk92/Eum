using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.Connections.Spotify.Models.Users;
using Eum.Enums;
using Flurl.Http.Configuration;

namespace Eum.Connections.Spotify.JsonConverters
{
    internal class MercuryTypeConverterToISpotifyItem : JsonConverter<ISpotifyItem[]>
    {
        public override ISpotifyItem[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            using var arrayEnumerator = jsonDoc.RootElement.EnumerateArray();
            var arrayData = arrayEnumerator.Select(elem => GetItem(elem, options))
                .ToArray();
            return arrayData;
        }

        public override void Write(Utf8JsonWriter writer, ISpotifyItem[] value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        private ISpotifyItem GetItem(JsonElement jsonObject, JsonSerializerOptions jsonSerializerOptions)
        {
            var uri = jsonObject.GetProperty("uri");
            var id = new SpotifyId(uri.GetString());

            switch (id.Type)
            {
                case EntityType.Playlist:
                    return jsonObject.Deserialize<MercurySearchPlaylist>(jsonSerializerOptions);
                case EntityType.Artist:
                    return jsonObject.Deserialize<MercurySearchArtist>(jsonSerializerOptions);
                case EntityType.Track:
                    return jsonObject.Deserialize<MercurySearchTrack>(jsonSerializerOptions);
                default:
                    return new EmptySpotifyItem(id);
            }
        }
    }

    public class MercurySearchPlaylist : ISpotifyItem
    {
        [JsonPropertyName("uri")]
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Id { get; init; }
        [JsonPropertyName("name")]
        public string Title { get; init; }
        public string Description => $"{Author}・{FollowersCount}";
        public ulong FollowersCount { get; init; }
        public string Author { get; init; }
        public string Image { get; init; }
    }

    public class EmptySpotifyItem : ISpotifyItem
    {
        public EmptySpotifyItem(SpotifyId id)
        {
            Id = id;
        }
        public SpotifyId Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string Image { get; }
    }
    public class MercurySearchArtist : ISpotifyItem
    {
        [JsonPropertyName("uri")]
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Id { get; init; }
        [JsonPropertyName("name")]
        public string Title { get; init; }
        public string Description => null;
        public string Image { get; init; }
    }

    public class MercurySearchTrack : ISpotifyItem
    {
        [JsonPropertyName("uri")]
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Id { get; init; }
        [JsonPropertyName("name")]
        public string Title { get; init; }
        public DiscographyTrackArtist[] Artists { get; init; }
        public DiscographyTrackArtist Album { get; init; }
        public string Description => $"{string.Join(", ", Artists.Select(z => z.Name))}・{Album.Name}";
        public string Image { get; init; }
        public double Duration { get; init; }
    }
}
