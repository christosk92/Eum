using Eum.Core.Contracts;
using Eum.Cores.Spotify.Contracts.Services;

namespace Eum.Cores.Spotify.Contracts.CoreConnection;

public interface ISpotifyCore : IMusicCore
{
    Task<bool> EnsureConnectedAsync(CancellationToken ct = default);
    ISpotifyBearerService BearerClient { get; }
}