using Eum.Artists;

namespace Eum.Core.Contracts.Models;

public interface ITrack
{
    /// <summary>
    /// (Required) The identifier for the track.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// (Required) The localized name of the track.
    /// </summary>
    string Title { get; }
    
    IAlbum Album { get; }
    
    IArtist[] Artists { get; }
}