namespace Eum.Cores.Spotify.Contracts.Services;

public interface ISpotifyBearerService
{
    ValueTask<string> GetBearerTokenAsync(CancellationToken ct = default);
}