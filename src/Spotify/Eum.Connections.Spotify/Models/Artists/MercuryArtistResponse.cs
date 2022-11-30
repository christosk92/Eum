using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.Connections.Spotify.Models.Users;
using Eum.Library.Contracts.JsonConverters;

namespace Eum.Connections.Spotify.Models.Artists;

/// <summary>
/// A resource object that represents the artist.
/// </summary>
public readonly struct MercuryArtist
{
    public MercuryArtist()
    {
    }

    /// <summary>
    /// The spotify uri for the artist.
    /// </summary>
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    [JsonPropertyName("uri")]
    public SpotifyId Uri { get; init; } = default;

    /// <summary>
    /// Information about the artist, including name, portraits and verified status.
    /// </summary>
    [JsonPropertyName("info")]
    public MercuryArtistInfo Info { get; init; } = default;

    /// <summary>
    /// The header image of the artist.
    /// </summary>
    [JsonConverter(typeof(MercuryArtistHeaderConverter))]
    [JsonPropertyName("header_image")]
    public string? Header { get; init; } = null;

    /// <summary>
    /// The artist's top tracks.
    /// </summary>
    [JsonConverter(typeof(MercuryArtistTopTrackConverter))]
    [JsonPropertyName("top_tracks")]
    public IEnumerable<MercuryArtistTopTrack> TopTracks { get; init; } = Enumerable.Empty<MercuryArtistTopTrack>();

    [JsonConverter(typeof(MercuryArtistRelatedArtistConverter))]
    [JsonPropertyName("related_artists")]
    public IEnumerable<MercuryArtistRelatedArtist> RelatedArtists { get; init; } = Enumerable.Empty<MercuryArtistRelatedArtist>();


    [JsonPropertyName("biography")] public BiographyWrapper? Biography { get; init; } = null;

    [JsonConverter(typeof(ReleasesToGroupedConverter))]
    [JsonPropertyName("releases")]
    public IDictionary<DiscographyType, IList<DiscographyRelease>> DiscographyReleases { get; init; } = null;

    [JsonPropertyName("latest_release")] public DiscographyRelease? LatestRelease { get; init; } = null;

    [JsonConverter(typeof(MercuryArtistPlaylistConverter))]
    [JsonPropertyName("published_playlists")]
    public IEnumerable<MercuryArtistPlaylist> Playlists { get; init; } = Enumerable.Empty<MercuryArtistPlaylist>();

    [JsonConverter(typeof(MonthlyListenersConverter))]
    [JsonPropertyName("monthly_listeners")]
    public long MonthlyListeners { get; init; } = 0;

    [JsonConverter(typeof(GalleryConverter))]
    [JsonPropertyName("gallery")]
    public IList<string> Gallery { get; init; } = new List<string>();

    [JsonPropertyName("pinned_item")] public PinnedItem? PinnedItem { get; init; } = null;

    [JsonPropertyName("merch")]
    [JsonConverter(typeof(MerchandiseItemConverter))]
    public IList<MerchandiseItem> Merch { get; init; } = new List<MerchandiseItem>(0);
}

public readonly struct BiographyWrapper
{
    [JsonPropertyName("text")] public string? Text { get; init; }
}