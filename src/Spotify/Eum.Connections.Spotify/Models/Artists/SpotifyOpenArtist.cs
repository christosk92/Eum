using System.Text.Json.Serialization;
using Eum.Artists;
using Eum.Artwork;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models.Artists;

public class SpotifyOpenArtist : IArtist
{
    /// <summary>
    /// The total number of followers.
    /// </summary>
    [JsonConverter(typeof(FollowersWrapperToFollowersConverter))]
    public int Folllowers { get; init; }
    
    /// <summary>
    /// A list of the genres the artist is associated with. If not yet classified, the array is empty.
    /// </summary>
    public string[] Genres { get; init; }
    /// <summary>
    /// The Spotify ID for the artist.
    /// </summary>
    public string Id { get; init; }
    /// <summary>
    /// The Spotify URI for the artist.
    /// </summary>
    [JsonConverter(typeof(UriToSpotifyIdConverter))]
    public SpotifyId Uri { get; init; }
    [JsonPropertyName("images")]
    [JsonConverter(typeof(SpotifyUrlImageToArtworkConverter))]
    public IArtwork[] Artwork { get; init; }
    public string Name { get; init; }
    
    /// <summary>
    /// The popularity of the artist. The value will be between 0 and 100, with 100 being the most popular.
    /// The artist's popularity is calculated from the popularity of all the artist's tracks.
    /// </summary>
    public int Popularity { get; init; }
}