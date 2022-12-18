using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Artists;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Albums
{
    public class MercuryAlbum
    {
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Uri { get; init; }
        public string Name { get; init; }
        public UriImage Cover { get; init; }
        public ushort Year { get; init; }
        public ushort? Month { get; init; }
        public ushort? Day { get; init; }
        public string Type { get; init; }
        public string Label { get; init; }
        public string[] Copyrights { get; init; }
        public DiscographyTrackArtist[] Artists { get; init; }
        [JsonPropertyName("track_count")]
        public ushort TrackCount { get; init; }
        [JsonConverter(typeof(DiscToListConverter))]
        [JsonPropertyName("discs")]
        public IList<IList<DiscographyTrackRelease>>? Discs { get; init; }
        // public GenericReleasesWrapper<MercuryArtistDiscographyRelease>? Additional { get; }
        //
        // [CanBeNull]
        // public GenericReleasesWrapper<MercuryArtistDiscographyRelease> Related { get; }
    }

    public class GenericReleasesWrapper<T>
    {
        [JsonConstructor]
        public GenericReleasesWrapper(T[] releases)
        {
            Releases = releases;
        }

        public T[] Releases { get; }
    }
}
