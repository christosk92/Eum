using System;
using System.Collections.Generic;
using System.Linq;
using Eum.Spotify.metadata;
using Google.Protobuf.Collections;

namespace Eum.Connections.Spotify.Enums;

public enum AudioQuality
{
    NORMAL,
    HIGH,
    VERY_HIGH
}

public static class AudioQualityExt
{
    public static AudioQuality GetQuality(this AudioFile.Types.Format format)
    {
        switch (format) {
            case AudioFile.Types.Format.Mp396:
            case AudioFile.Types.Format.OggVorbis96:
                return AudioQuality.NORMAL;
            case AudioFile.Types.Format.Mp3160:
            case AudioFile.Types.Format.Mp3160Enc:
            case AudioFile.Types.Format.OggVorbis160:
            case AudioFile.Types.Format.Aac24:
                return AudioQuality.HIGH;
            case AudioFile.Types.Format.Mp3320:
            case AudioFile.Types.Format.Mp3256:
            case AudioFile.Types.Format.OggVorbis320:
            case AudioFile.Types.Format.Aac48:
                return AudioQuality.VERY_HIGH;
            default:
                throw new ArgumentOutOfRangeException("Unknown format: " + format);
        }
    }

    public static IEnumerable<AudioFile> GetMatches(this RepeatedField<AudioFile> files, AudioQuality quality)
    {
        return files
            .Where(a => a.HasFormat && a.Format.GetQuality() == quality);
    }
}