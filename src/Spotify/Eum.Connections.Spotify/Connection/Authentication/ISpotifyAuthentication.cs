using Eum.Spotify;

namespace Eum.Connections.Spotify.Connection.Authentication;

public interface ISpotifyAuthentication
{
    LoginCredentials GetCredentials();
}