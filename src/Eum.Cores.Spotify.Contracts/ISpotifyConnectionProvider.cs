namespace Eum.Cores.Spotify.Contracts;

public interface ISpotifyConnectionProvider
{
    Task<ISpotifyConnection>
        GetConnectionAsync(CancellationToken ct);
}