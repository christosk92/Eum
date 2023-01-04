using System.Collections.Concurrent;
using System.Text.Json;
using Eum.Connections.Spotify.Clients.Contracts;
using Flurl;
using Flurl.Http;

namespace Eum.Connections.Spotify.Clients
{
    public sealed class ExtractedColorClient : IExtractedColorsClient
    {
        private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<ColorTheme, string>> _colorsCache = new();
        private readonly IBearerClient _bearerClient;
        public ExtractedColorClient(IBearerClient bearerClient)
        {
            _bearerClient = bearerClient;
        }
        public async ValueTask<IReadOnlyDictionary<ColorTheme, string>> GetColors(string image, CancellationToken ct = default)
        {
            if (_colorsCache.TryGetValue(image, out var c))
            {
                return c;
            }
            var bearerToken = await _bearerClient.GetBearerTokenAsync(ct);
            //https://api-partner.spotify.com/pathfinder/v1/query?operationName=fetchExtractedColors&variables=%7B%22uris%22%3A%5B%22https%3A%2F%2Fdailymix-//images.scdn.co%2Fv2%2Fimg%2Fab6761610000e5eb30fb47dd22deb36bf0175501%2F2%2Fen%2Fdefault%22%5D%7D&extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%22d7696dd106f3c84a1f3ca37225a1de292e66a2d5aced37a66632585eeb3bbbfa%22%7D%7D

            //{"uris":["https://dailymix-images.scdn.co/v2/img/ab6761610000e5eb30fb47dd22deb36bf0175501/2/en/default"]}
            //{"persistedQuery":{"version":1,"sha256Hash":"d7696dd106f3c84a1f3ca37225a1de292e66a2d5aced37a66632585eeb3bbbfa"}}
            using var stream = await "https://api-partner.spotify.com"
                .AppendPathSegments("pathfinder", "v1", "query")
                .SetQueryParam("operationName", "fetchExtractedColors")
                .SetQueryParam("variables", JsonSerializer.Serialize(new
                {
                    uris = new[]
                    {
                        image
                    }
                }))
                .SetQueryParam("extensions", JsonSerializer.Serialize(new
                {
                    persistedQuery = new
                    {
                        version = 1,
                        sha256Hash = "d7696dd106f3c84a1f3ca37225a1de292e66a2d5aced37a66632585eeb3bbbfa"
                    }
                }))
                .WithOAuthBearerToken(bearerToken)
                .GetStreamAsync(cancellationToken: ct);

            using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            //data -> extractedColors -> {colorRaw, colorDark, colorLight}
            var colors = jsonDoc.RootElement.GetProperty("data")
                .GetProperty("extractedColors");

            using var arr = colors.EnumerateArray();
            var result = new Dictionary<ColorTheme, string>();
            var colorsData = arr.FirstOrDefault();

            result[ColorTheme.Raw] = colorsData.GetProperty("colorRaw")
                .GetProperty("hex")
                .GetString()!;

            result[ColorTheme.Dark] = colorsData.GetProperty("colorDark")
                .GetProperty("hex")
                .GetString()!;

            result[ColorTheme.Light] = colorsData.GetProperty("colorLight")
                .GetProperty("hex")
                .GetString()!;
            _colorsCache[image] = result;
            return result;
        }
    }

    public enum ColorTheme
    {
        Dark,
        Light,
        Raw
    }
}
