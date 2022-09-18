using CPlayerLib;

namespace Eum.Cores.Spotify.Contracts.CoreConnection;

public interface ISpotifyConnectionFactory
{
    ISpotifyConnection GetNewConnection(LoginCredentials loginCredentials);
}