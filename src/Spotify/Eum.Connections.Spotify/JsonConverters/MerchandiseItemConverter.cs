using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models.Artists;

namespace Eum.Connections.Spotify.JsonConverters;

public class MerchandiseItemConverter : JsonConverter<IList<MerchandiseItem>>
{
    public override IList<MerchandiseItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        reader.Read();
        var items = JsonSerializer.Deserialize<IList<MerchandiseItem>>(ref reader, options);
        reader.Read();
        return items;
    }

    public override void Write(Utf8JsonWriter writer, IList<MerchandiseItem> value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}