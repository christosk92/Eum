using Connectstate;

namespace Eum.Cores.Spotify.Contracts.Connect;

public interface ISpotifyRemoteConnection : IDisposable
{
    bool IsAlive { get; }
    string? ConnectionId { get; }
    Task<bool> EnsureConnectedAsync(CancellationToken ct);
    
    Cluster? PreviousCluster { get; }
    
    event EventHandler<ClusterUpdate?>? ClusterUpdated;
    event EventHandler<string>? Disconnected; 
}