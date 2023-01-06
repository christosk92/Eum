using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.Audio;
using Eum.Connections.Spotify.Playback.States;
using Eum.Spotify.connectstate;

namespace Eum.Connections.Spotify.Playback;

public interface ISpotifyPlaybackClient
{
    void AddEventsListener(IEventsListener listener);
    void RemoveEventsListener(IEventsListener listener);
    StateWrapper State { get; }
    MetadataWrapper CurrentMetadata { get; }
    ValueTask<int> Time { get; }
    Cluster LatestCluster { get; }

    event EventHandler<ClusterUpdate> ClusterChanged;

    Task PlayOnDevice(SpotifyId contextId,
        SpotifyId? trackUri = null,
        int? trackIndex = null,
        string? deviceId = null, CancellationToken ct = default);
}