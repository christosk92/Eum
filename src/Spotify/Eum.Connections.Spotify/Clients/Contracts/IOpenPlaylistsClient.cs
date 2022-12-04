using Eum.Connections.Spotify.Attributes;
using Eum.Connections.Spotify.Models.Artists;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Eum.Connections.Spotify.Clients.Contracts;
[OpenUrlEndpoint]
public interface IOpenPlaylistsClient
{
    /// <summary>
    /// Get Spotify catalog information for a single artist identified by their unique Spotify ID.
    /// </summary>
    /// <param name="id">The Spotify ID of the artist.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Post("/users/{user_id}/playlists")]
    Task<SpotifyOpenPlaylist> CreatePlaylist(string user_id, [Body]CreatePlaylistRequest playlistRequest, CancellationToken ct = default);

    /// <summary>
    /// Get Spotify catalog information for a single artist identified by their unique Spotify ID.
    /// </summary>
    /// <param name="id">The Spotify ID of the artist.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    //[Header("Content-Type: image/jpeg")]
    [Put("/playlists/{playlistId}/images")]
    Task UploadImage(string playlistId, [Body]string imageBase64, CancellationToken ct = default);
}

public class SpotifyOpenPlaylist
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class CreatePlaylistRequest  
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("public")]
    public bool Public { get; set; }
    [JsonPropertyName("collaborative")]
    public bool Collaborative { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
}