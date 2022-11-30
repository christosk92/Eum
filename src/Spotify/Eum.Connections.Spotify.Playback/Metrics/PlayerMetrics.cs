using System;
using Eum.Connections.Spotify.Playback.Audio;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Crossfade;
using Eum.Connections.Spotify.Playback.Enums;

namespace Eum.Connections.Spotify.Playback.Metrics;

public class PlayerMetrics
{
    public PlayableContentMetrics? ContentMetrics { get; set; }
    public int DecodedLength { get; private set; } = 0;

    public int Size { get; private set; } = 0;

    public int Bitrate { get; private set; } = 0;

    public float SampleRate { get; private set; } = 0;
    public int Duration { get; private set; } = 0;

    public string? Encoding { get; private set; } = null;

    public int FadeOverlap { get; private set; } = 0;
    public string Transition { get; private set; } = "none";
    public int DecryptTime { get; private set; } = 0;

    public PlayerMetrics(PlayableContentMetrics contentMetrics, CrossfadeController crossFade,
        IDecodedAudioStream? stream)
    {
        ContentMetrics = contentMetrics;

        // if (decoder != null)
        // {
        //     Size = decoder.Size;
        //     Duration = decoder.Duration;
        //
        //     var format = decoder.AudioFormat;
        //
        //     Bitrate = (int) (format.FrameRate * format.FrameSize);
        //     SampleRate = format.SampleRate;
        // }

        if (stream != null)
        {
            DecryptTime = stream.DecryptTimeMs();
            DecodedLength = stream.DecodedLength;

            Encoding = stream.Codec switch
            {
                SuperAudioFormat.Mp3 => "mp3",
                SuperAudioFormat.Vorbis => "vorbis",
                SuperAudioFormat.Aac => "aac",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        if (crossFade is
            {
                IsSupported: true
            })
        {
            Transition = "crossfade";
            FadeOverlap = crossFade.FadeOverlap;
        }
    }
}