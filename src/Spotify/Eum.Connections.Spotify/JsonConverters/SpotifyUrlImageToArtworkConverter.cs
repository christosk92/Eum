using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Artwork;
using Eum.Connections.Spotify.Models.Images;

namespace Eum.Connections.Spotify.JsonConverters;

public class SpotifyUrlImageToArtworkConverter : JsonConverter<IArtwork[]>
{
    public override IArtwork[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //array of images with url, height and width
        var images = JsonSerializer.Deserialize<IEnumerable<SpotifyUrlImage>>(ref reader, options);
        return images?.Cast<IArtwork>()?.ToArray()
            ?? Array.Empty<IArtwork>();
    }

    public override void Write(Utf8JsonWriter writer, IArtwork[] value, JsonSerializerOptions options)
    {
        //perform the reverse:
        var images = value.Cast<SpotifyUrlImage>();
        JsonSerializer.Serialize(writer, images, options);
    }
}