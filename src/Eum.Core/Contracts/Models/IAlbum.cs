namespace Eum.Core.Contracts.Models;

public interface IAlbum
{
    /// <summary>
    /// (Required) The identifier for the album.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// (Required) The localized name of the album.
    /// </summary>
    string Title { get; }
    /// <summary>
    /// The artwork for the image.
    /// </summary>
    IArtwork[] Artwork { get; }
}