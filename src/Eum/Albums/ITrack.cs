using Eum.Artists;
using Eum.Core.Contracts.Models;

namespace Eum.Albums;

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
    int Duration { get; }
    IArtist[] Artists { get; }
}