using Connectstate;
using Eum.Cores.Spotify.Contracts.Models;

namespace Eum.Cores.Spotify.Contracts.Connect;

public interface ISpotifyRemote
{
    Cluster? LatestReceivedCluster { get; }

    event EventHandler<ClusterUpdate?> ClusterUpdated;
    event TypedEventHandler<ISpotifyRemoteReconnectOption, EventArgs>? Disconnected;

    ValueTask<bool> EnsureConnectedAsync(CancellationToken ct = default);
    ValueTask<CurrentlyPlayingState?> GetCurrentlyPlayingAsync(CancellationToken stoppingToken = default);
}