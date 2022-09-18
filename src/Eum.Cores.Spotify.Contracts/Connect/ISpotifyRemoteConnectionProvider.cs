namespace Eum.Cores.Spotify.Contracts.Connect;

public interface ISpotifyRemoteConnectionProvider
{
    ValueTask<ISpotifyRemoteConnection> GetConnectionAsync(CancellationToken ct = default);
}