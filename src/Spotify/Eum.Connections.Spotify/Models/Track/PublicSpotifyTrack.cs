using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Albums;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Track
{
    public class PublicSpotifyTrack : ISpotifyItem
    {
        /// <summary>
        /// The Spotify URI for the artist.
        /// </summary>
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        [JsonPropertyName("uri")]
        public SpotifyId Id { get; init; }
        [JsonPropertyName("name")]
        public string Title { get; init; }

        public SpotifyOpenAlbum Album { get; init; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; init; }
        public bool Explicit { get; init; }
        [JsonPropertyName("is_local")]
        public bool Local { get; init; }
        public DiscographyTrackArtist[] Artists { get; init; }
        public string Description => string.Join(", ", Artists.Select(a => a.Name));
        public string Image => Album.Image;
    }
}
