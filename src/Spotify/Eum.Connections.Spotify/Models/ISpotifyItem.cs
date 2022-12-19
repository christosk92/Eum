using System;
using System.Collections.Generic;
using System.Text;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Models
{
    public interface ISpotifyItem
    {
        SpotifyId Id { get; }
        string Title { get; }
        string Description { get; }
        string Image { get; }
    }
}
