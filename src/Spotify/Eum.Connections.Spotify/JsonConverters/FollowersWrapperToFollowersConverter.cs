using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eum.Connections.Spotify.JsonConverters;

public class FollowersWrapperToFollowersConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //total is inside a wrapper object called followers
        reader.Read(); //start object
        reader.Read(); //href
        reader.Read(); //href value
        reader.Read(); // property name
        var total = reader.GetInt32();
        reader.Read();
        return total;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}