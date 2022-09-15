namespace Eum.Core.Contracts.Models;
/// <summary>
/// An object that represents artwork.
/// </summary>
public interface IArtwork
{
    /// <summary>
    /// (Required) The maximum width available for the image.
    /// </summary>
    ushort Height { get; }
    
    /// <summary>
    /// (Required) The maximum width available for the image.
    /// </summary>
    uint Width { get; }
    
    string Url { get; }
}