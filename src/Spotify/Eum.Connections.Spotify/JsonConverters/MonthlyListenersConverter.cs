using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eum.Library.Contracts.JsonConverters;

public class MonthlyListenersConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        //the playlists are wrapped inside a published_playlists wrapper object.
        //we need to skip that object and read the playlists array.
        reader.Read();
        reader.Read();
        var listeners =  reader.GetInt64();
        reader.Read();
        return listeners;
    }

    public override void Write(Utf8JsonWriter writer, long value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}