namespace Eum.Core.Contracts.Models;

public interface IArtist : ICoreResponse
{
    /// <summary>
    /// (Required) The identifier for the artist.
    /// </summary>
    string Id { get; }
    /// <summary>
    /// (Required) The localized name of the artist.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// The artwork for the artist image.
    /// </summary>
    IArtwork? Artwork { get; }
}
