using Eum.Core.Contracts;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Contracts.Services;

namespace Eum.Cores.Spotify.Contracts.CoreConnection;

public interface ISpotifyCore : IMusicCore
{
    Task<bool> EnsureConnectedAsync(CancellationToken ct = default);
    ISpotifyBearerService BearerClient { get; }
    ISpotifyClientsProvider ClientsProvider { get; }
    SpotifyConfig Config { get; }
}