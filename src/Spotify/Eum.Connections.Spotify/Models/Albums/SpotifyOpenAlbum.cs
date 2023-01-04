using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Eum.Artwork;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.Connections.Spotify.Models.Users;
using Eum.Spotify.metadata;

namespace Eum.Connections.Spotify.Models.Albums
{
    public class SpotifyOpenAlbum : ISpotifyItem
    {    /// <summary>
         /// The Spotify URI for the artist.
         /// </summary>
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        [JsonPropertyName("uri")]
        public SpotifyId Id { get; init; }

        [JsonPropertyName("images")]
        [JsonConverter(typeof(SpotifyUrlImageToArtworkConverter))]
        public IArtwork[] Artwork { get; init; }

        [JsonPropertyName("name")]
        public string Title
        {
            get; init;
        }

        public DiscographyTrackArtist[] Artists { get; init; }

        [JsonPropertyName("album_type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Album.Types.Type AlbumType { get; init; }

        public string Description => AlbumType.ToString();
        public string Image => Artwork?.FirstOrDefault()?.Url;

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; init; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("release_date_precision")]
        public ReleaseDatePrecisionType ReleaseDatePrecision { get; init; }
        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; init; }
    }

    public enum ReleaseDatePrecisionType
    {
        Year,
        Month,
        Day
    }
}
