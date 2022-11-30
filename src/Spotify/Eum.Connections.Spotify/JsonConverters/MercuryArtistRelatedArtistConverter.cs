using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models.Artists;

namespace Eum.Connections.Spotify.JsonConverters;

internal class MercuryArtistRelatedArtistConverter : JsonConverter<IEnumerable<MercuryArtistRelatedArtist>>
{
    public override IEnumerable<MercuryArtistRelatedArtist>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        //there is a bug in the spotify api where the related artists are not returned as an array but inside a wrapper object
        //we need to read the wrapper object and then read the array
        //making sure to return EndObject so the deserializer can continue
        reader.Read();
        reader.Read();
        var result = JsonSerializer.Deserialize<IEnumerable<MercuryArtistRelatedArtist>>(ref reader, options);
        reader.Read();
        return result;
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<MercuryArtistRelatedArtist> value,
        JsonSerializerOptions options)
    {
        //write as the same wrapper object
        writer.WriteStartObject();
        writer.WritePropertyName("artists");
        JsonSerializer.Serialize(writer, value, options);
    }
}