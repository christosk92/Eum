using Eum.Connections.Spotify.Playback.Audio;
using Eum.Connections.Spotify.Playback.States;
using Eum.Spotify.connectstate;

namespace Eum.Connections.Spotify.Playback;

public interface ISpotifyPlaybackClient
{
    void AddEventsListener(IEventsListener listener);
    void RemoveEventsListener(IEventsListener listener);
    StateWrapper State { get; }
    MetadataWrapper CurrentMetadata { get;  }
    int Time { get; }
    Cluster LatestCluster { get; }
}