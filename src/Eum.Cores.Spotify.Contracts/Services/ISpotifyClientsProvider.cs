namespace Eum.Cores.Spotify.Contracts.Services;

public interface ISpotifyClientsProvider
{
    ValueTask<ISpClient> SpClient(CancellationToken ct = default);
}