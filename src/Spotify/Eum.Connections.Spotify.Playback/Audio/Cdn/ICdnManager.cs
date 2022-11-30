using System;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Spotify.metadata;
using Google.Protobuf;

namespace Eum.Connections.Spotify.Playback.Audio.Cdn;

public interface ICdnManager
{
    AudioFile File { get; }
    byte[] Key { get; }
    string Url { get; }
    CdnAudioStreamer StreamFile(IHaltListener haltListener, string name);
    Task<Uri> GetAudioUrl(ByteString fileId);
}