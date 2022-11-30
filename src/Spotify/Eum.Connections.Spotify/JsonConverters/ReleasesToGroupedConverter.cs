using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models.Artists.Discography;

namespace Eum.Connections.Spotify.JsonConverters;

public class ReleasesToGroupedConverter : JsonConverter<IDictionary<DiscographyType, IList<DiscographyRelease>>>
{
    private static IReadOnlyDictionary<string, DiscographyType> Mappings = new Dictionary<string, DiscographyType>
    {
        {"albums", DiscographyType.Album},
        {"singles", DiscographyType.Single},
        {"compilations", DiscographyType.Compilation},
        {"appears_on", DiscographyType.AppearsOn}
    };

    public override IDictionary<DiscographyType, IList<DiscographyRelease>>? Read(ref Utf8JsonReader reader,
        Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var value = new Dictionary<DiscographyType, IList<DiscographyRelease>>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (value.Count == Mappings.Count)
                {
                    reader.Read();
                    return value;
                }
                //skip
                continue;
            }

            var keyString = reader.GetString();

            if (!Mappings.TryGetValue(keyString, out var result))
            {
                throw new JsonException($"Unable to convert \"{keyString}\" to DeliveryType.");
            }

            reader.Read();
            reader.Read();
            if (reader.GetString() == "releases")
            {
                var releases = JsonSerializer.Deserialize<IList<DiscographyRelease>?>(ref reader, options);
                value[result] = releases ?? new List<DiscographyRelease>(0);
                reader.Read();
                reader.Read();
            }
            else
            {
                value[result] = new List<DiscographyRelease>(0);
                reader.Read();
            }

            //and skip total_count field
        }

        throw new JsonException("Error Occured");
    }

    public override void Write(Utf8JsonWriter writer, IDictionary<DiscographyType, IList<DiscographyRelease>> value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}