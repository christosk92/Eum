namespace Eum.Cores.Spotify.Contracts.Models;

public interface ISpotifyItem 
{
    /// <summary>
    /// (Required) The spotify uri for the item.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// The localized title of the object.
    /// </summary>
    string Title { get; }
}