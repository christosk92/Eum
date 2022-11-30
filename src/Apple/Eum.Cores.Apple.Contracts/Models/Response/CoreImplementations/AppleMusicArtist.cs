using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Eum.Artists;
using Eum.Artwork;
using Eum.Core.Contracts;
using Eum.Core.Contracts.Models;

namespace Eum.Cores.Apple.Contracts.Models.Response.CoreImplementations;

/// <summary>
/// A resource object that represents the artist of an album where an artist can be one or more people.
/// </summary>
public sealed class AppleMusicArtist : IArtist
{
    /// <inheritdoc />
    [Required]
    public string Id
    {
        get;
        init;
    } = null!;

    
    /// <inheritdoc/>
    public string Name => Attribute.Name;

    /// <inheritdoc/>
    public IArtwork[] Artwork => new[] { Attribute.Artwork };

    /// <summary>
    /// (Required) This value is always artists.
    /// </summary>
    [Required]
    public string Type
    {
        get;
        init;
    } = null!;

    /// <summary>
    /// (Required) The relative location for the artist resource.
    /// </summary>
    [Required]
    public string Href
    {
        get;
        init;
    } = null!;

    /// <summary>
    /// The attributes for the artist.
    /// </summary>
    [JsonPropertyName("attributes")]
    public AppleMusicArtist.Attributes Attribute
    {
        get;
        init;
    } = null!;


    /// <summary>
    /// The attributes for an artist resource.
    /// </summary>
    public sealed class Attributes
    {
        /// <summary>
        /// The artwork for the artist image.
        /// </summary>
        public AppleMusicArtwork? Artwork
        {
            get;
            init;
        }

        /// <summary>
        /// The notes about the artist that appear in the Apple Music catalog.
        /// </summary>
        [Required]
        public EditorialNotes? EditorialNotes
        {
            get;
            init;
        } = null!;

        /// <summary>
        /// (Required) The names of the genres associated with this artist.
        /// </summary>
        [Required]
        public string[] GenreNames
        {
            get;
            init;
        } = null!;

        /// <summary>
        /// (Required) The localized name of the artist.
        /// </summary>
        [Required]
        public string Name
        {
            get;
            init;
        } = null!;

        /// <summary>
        /// (Required) The URL for sharing the album in Apple Music.
        /// </summary>
        [Required]
        public string Url
        {
            get;
            init;
        } = null!;
    }

    public bool IsError => false;
}
