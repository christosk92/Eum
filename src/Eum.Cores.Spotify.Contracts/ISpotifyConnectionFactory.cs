using CPlayerLib;

namespace Eum.Cores.Spotify.Contracts;

public interface ISpotifyConnectionFactory
{
    ISpotifyConnection GetNewConnection(LoginCredentials loginCredentials);
}