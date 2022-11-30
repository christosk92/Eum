using Eum.Spotify.metadata;

namespace Eum.Connections.Spotify.Playback.Enums;

public enum SuperAudioFormat
{
    Vorbis,
    Mp3,
    Aac,
    Unknown
}

public static class SuperAudioFormatExt
{
    public static SuperAudioFormat GetFormat(this AudioFile.Types.Format format)
    {
        switch (format)
        {
            case AudioFile.Types.Format.OggVorbis96:
            case AudioFile.Types.Format.OggVorbis160:
            case AudioFile.Types.Format.OggVorbis320:
                return SuperAudioFormat.Vorbis;
            case AudioFile.Types.Format.Mp3256:
            case AudioFile.Types.Format.Mp3320:
            case AudioFile.Types.Format.Mp3160:
            case AudioFile.Types.Format.Mp396:
            case AudioFile.Types.Format.Mp3160Enc:
                return SuperAudioFormat.Mp3;
            case AudioFile.Types.Format.Aac24:
            case AudioFile.Types.Format.Aac48:
                return SuperAudioFormat.Aac;
            default:
                return SuperAudioFormat.Unknown;
        }
    }
}