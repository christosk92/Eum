using Connectstate;
using Eum.Cores.Spotify.Contracts.Helpers;

namespace Eum.Cores.Spotify.Contracts.Connect;

public interface ISpotifyRemote
{
    Cluster? LatestReceivedCluster { get; }

    event EventHandler<ClusterUpdate?> ClusterUpdated;
    event TypedEventHandler<ISpotifyRemoteReconnectOption, EventArgs>? Disconnected;

    Task<bool> EnsureConnectedAsync(CancellationToken ct = default);
}