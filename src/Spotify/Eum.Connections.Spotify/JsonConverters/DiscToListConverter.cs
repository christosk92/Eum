using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models.Artists.Discography;

namespace Eum.Connections.Spotify.JsonConverters;

public class DiscToListConverter : JsonConverter<IList<IList<DiscographyTrackRelease>>>
{
    public override IList<IList<DiscographyTrackRelease>>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        //the incoming json is defined as follows:
        /*
         * "discs": [
            {
              "number": 1,
              "name": "",
              "tracks": []
            }
          ]
         */
        //we need to extract tracks from all possible discs and return a list of list of tracks
        var discs = new List<IList<DiscographyTrackRelease>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "tracks")
            {
                reader.Read();
                var disc = JsonSerializer.Deserialize<List<DiscographyTrackRelease>>(ref reader, options);
                discs.Add(disc);
            }
        }
        return discs;
    }

    public override void Write(Utf8JsonWriter writer, IList<IList<DiscographyTrackRelease>> value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}