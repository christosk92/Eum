using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Spotify.metadata;

namespace Eum.Connections.Spotify.Playback.Audio;

public class LoadedStream
{

    public LoadedStream(Track track, IDecodedAudioStream streamer,
        object? normalizationData, 
        PlayableContentMetrics metrics, MetadataWrapper metadata)
    {
        Track = track;
        Streamer = streamer;
        NormalizationData = normalizationData;
        Metrics = metrics;
        Metadata = metadata;
    }
    
    public PlayableContentMetrics Metrics { get; }
    public object? NormalizationData { get; }
    public IDecodedAudioStream Streamer { get; }
    public Track Track { get; }

    public MetadataWrapper Metadata { get; }
}