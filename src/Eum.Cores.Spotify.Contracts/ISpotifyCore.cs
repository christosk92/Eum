using Eum.Core.Contracts;

namespace Eum.Cores.Spotify.Contracts;

public interface ISpotifyCore : IMusicCore
{
    Task<bool> EnsureConnectedAsync(CancellationToken ct = default);
}