namespace Eum.Cores.Spotify.Contracts.Connect;

public interface ISpotifyRemoteConnection : IDisposable
{
    bool IsAlive { get; }
    Task<bool> EnsureConnectedAsync(CancellationToken ct);
}