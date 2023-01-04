using System.Text.Json.Serialization;

namespace Eum.Connections.Spotify.Models.Views
{
    public readonly struct View<T>
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
        [JsonPropertyName("tag_line")]
        public string? TagLine { get; init; }
        [JsonPropertyName("rendering")]
        public string Rendering { get; init; }
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("content")]
        public ViewContent<T> Content { get; init; }
    }

    public readonly struct ViewContent<T>
    {
        [JsonPropertyName("items")]
        public IEnumerable<T>? Items { get; init; }
        public int Total { get; init; }
    }
}
