namespace Eum.Cores.Spotify.Contracts.CoreConnection;

public interface ISpotifyConnectionProvider
{
    Task<ISpotifyConnection>
        GetConnectionAsync(CancellationToken ct);
}