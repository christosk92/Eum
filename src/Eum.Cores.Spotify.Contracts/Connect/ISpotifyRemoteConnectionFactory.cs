namespace Eum.Cores.Spotify.Contracts.Connect;

public interface ISpotifyRemoteConnectionFactory
{
    ISpotifyRemoteConnection GetConnection(string websocketUrl);
}