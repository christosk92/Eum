using Connectstate;

namespace Eum.Cores.Spotify.Contracts.Connect;

public interface ISpotifyRemote
{
    Cluster? LatestReceivedCluster { get; }

    event EventHandler<ClusterUpdate?> ClusterUpdated;

    Task<bool> EnsureConnectedAsync(CancellationToken ct = default);
}