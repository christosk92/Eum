using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eum.Library.Contracts.JsonConverters;

public class GalleryConverter : JsonConverter<IList<string>>
{
    public override IList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //the json object MAY contain the "images" array which contains an object with the property "uri"
        //we want to extract the "uri" property from each object in the array and return a list of strings
        //if the "images" array is not present, return an empty list
        
        //if the json object does not contain the "images" array, return an empty list
        //if the json object does contain the "images" array, read the array
        var images = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                reader.Read();
                break;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }
                    if (reader.GetString() == "uri")
                    {
                        reader.Read();
                        images.Add(reader.GetString());
                        //break;
                    }
                }
            }
        }
        return images;
    }

    public override void Write(Utf8JsonWriter writer, IList<string> value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}