using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models.Artists;

namespace Eum.Connections.Spotify.JsonConverters;

public class MercuryArtistTopTrackConverter : JsonConverter<IEnumerable<MercuryArtistTopTrack>>
{
    public override IEnumerable<MercuryArtistTopTrack>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        //there is a bug in the json response, where the top tracks are not in an array but instead inside a wrapper object
        //so we need to read the object and then read the array
        //and make sure to skip the closing bracket of the object
        reader.Read();
        reader.Read();
        var tracks = JsonSerializer.Deserialize<IEnumerable<MercuryArtistTopTrack>>(ref reader, options);
        reader.Read();
        return tracks;
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<MercuryArtistTopTrack> value,
        JsonSerializerOptions options)
    {
        //write the array as the same wrapper object
        writer.WriteStartObject();
        writer.WritePropertyName("tracks");
        JsonSerializer.Serialize(writer, value, options);
        writer.WriteEndObject();
    }
}