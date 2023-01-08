using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Attributes;
using Refit;

namespace Eum.Connections.Spotify.Clients.Contracts
{
    [SpClientEndpoint]
    public interface IColorLyrics
    {
        //https://spclient.wg.spotify.com/color-lyrics/v2/track/6WeCNrVIIGnmWH9LX5NpeH?format=json&vocalRemoval=false&market=from_token
        [Get("/color-lyrics/v2/track/{trackId}?format=json&vocalRemoval=false&market=from_token")]
        [Headers("app-platform: WebPlayer")]
        Task<LyricsResponse> GetLyrics(string trackId, CancellationToken ct = default);
    }

    public class LyricsResponse 
    {
        public LyricsObject Lyrics { get; init; }
    }

    public class LyricsObject
    {
        [JsonPropertyName("syncType")]
        public string SyncType { get; init; }
        public LyricsLine[]? Lines { get; init; }
    }

    public class LyricsLine
    {
        [JsonConverter(typeof(StringToDoubleConverter))]
        public double StartTimeMs { get; init; }
        public string Words { get; init; }
    }

    public class StringToDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Number) return reader.GetDouble();
            var str = reader.GetString();
            if (double.TryParse(str, out var r))
                return r;
            return default;
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
          writer.WriteStringValue(value.ToString());
        }
    }
}
