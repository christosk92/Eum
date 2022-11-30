using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.JsonConverters;

public class UriToSpotifyIdConverter : JsonConverter<SpotifyId>
{    
    private static readonly UTF8Encoding encoder = new();

    public override SpotifyId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //read the incoming reader as readonlyspan<char> and create a new SpotifyId from that.
        return new SpotifyId(reader.GetString().AsSpan());
    }

    public override void Write(Utf8JsonWriter writer, SpotifyId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Uri);
    }
}