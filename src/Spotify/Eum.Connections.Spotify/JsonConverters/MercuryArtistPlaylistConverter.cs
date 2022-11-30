using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models.Artists;

namespace Eum.Connections.Spotify.JsonConverters;

public class MercuryArtistPlaylistConverter : JsonConverter<IEnumerable<MercuryArtistPlaylist>>
{
    public override IEnumerable<MercuryArtistPlaylist>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        //the playlists are wrapped inside a published_playlists wrapper object.
        //we need to skip that object and read the playlists array.
        reader.Read();
        if (reader.TokenType == JsonTokenType.EndObject) return Enumerable.Empty<MercuryArtistPlaylist>();
        reader.Read();
        var playlists =
            JsonSerializer.Deserialize<IEnumerable<MercuryArtistPlaylist>>(ref reader, options);
        reader.Read();
        return playlists;
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<MercuryArtistPlaylist> value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}