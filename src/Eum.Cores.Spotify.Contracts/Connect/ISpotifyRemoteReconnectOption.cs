namespace Eum.Cores.Spotify.Contracts.Connect;

public interface ISpotifyRemoteReconnectOption
{
    Task<bool> ReconnectAsync(CancellationToken ct = default);
}