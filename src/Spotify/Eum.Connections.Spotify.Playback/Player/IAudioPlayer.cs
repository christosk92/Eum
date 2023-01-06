using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Enums;

namespace Eum.Connections.Spotify.Playback.Player;

public interface IAudioPlayer
{
    ValueTask Pause(string playbackId, bool releaseResources);
    ValueTask Resume(string playbackId);
    ValueTask InitStream(SuperAudioFormat codec,
        AbsChunkedInputStream audioStreamStream,
        float normalizationFactor,
        int duration,
        string playbackId, 
        long playFrom);

    ValueTask<int> Time(string playbackId);
    void Dispose(string playbackId);
    void Seek(string playbackId, int posInMs);

    event EventHandler<(string playbackId, int Time)> TimeChanged;
    event EventHandler<string> TrackFinished;
    void Gain(string playbackId, float getGain);

    event EventHandler<(string PlaybackId, PlaybackStateType)> StateChanged;

    /// <summary>
    /// Set the volume accordingly.
    /// </summary>
    /// <param name="playbackid">The playback whose volume should be changed.</param>
    /// <param name="volume">The volume value from 0 to 1, inclusive.</param>
    void SetVolume(string playbackid, float volume);

    void ReleaseAll()
    {
        throw new NotImplementedException();
    }
}

public enum PlaybackStateType
{
    Resumed,
    Paused,
    Seeked
}