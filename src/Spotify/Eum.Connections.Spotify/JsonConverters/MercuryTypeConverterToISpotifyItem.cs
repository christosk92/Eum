using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models;
using Eum.Connections.Spotify.Models.Users;
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
                default:
                    return new EmptySpotifyItem(id);
            }
        }
    }

    public class EmptySpotifyItem : ISpotifyItem
    {
        public EmptySpotifyItem(SpotifyId id)
        {
            Id = id;
        }
        public SpotifyId Id { get; }
    }
}
