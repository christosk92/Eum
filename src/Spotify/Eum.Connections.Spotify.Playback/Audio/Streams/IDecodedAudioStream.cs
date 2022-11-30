using Eum.Connections.Spotify.Playback.Enums;

namespace Eum.Connections.Spotify.Playback.Audio.Streams;

public interface IDecodedAudioStream : IDisposable
{
    AbsChunkedInputStream Stream { get; }

    SuperAudioFormat Codec { get; }
    int DecodedLength { get; }
    int DecryptTimeMs();
}