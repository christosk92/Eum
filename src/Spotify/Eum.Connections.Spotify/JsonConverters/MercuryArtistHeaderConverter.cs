using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eum.Connections.Spotify.JsonConverters;

internal class MercuryArtistHeaderConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? headerUrl = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "image":
                        reader.Read();
                        headerUrl = reader.GetString();
                        break;
                }
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return headerUrl;
            }
        }

        return headerUrl;
    }

    
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        //write the header url as the same wrapper object
        writer.WriteStartObject();
        writer.WriteString("image", value);
        writer.WriteEndObject();
    }
}