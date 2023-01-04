using System.Collections.Concurrent;
using System.Diagnostics;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Logging;
using NAudio.Utils;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Eum.Connections.Spotify.NAudio;

public class VorbisHolder : IDisposable
{
    public VorbisHolder(VorbisWaveReader reader, IWavePlayer waveOut, VolumeSampleProvider volumeSampler)
    {
        Reder = reader;
        WaveOut = waveOut;
        GainSampler = volumeSampler;
        _CancellationToken = new CancellationTokenSource();
    }

    public VorbisWaveReader Reder { get; }
    public IWavePlayer WaveOut { get; }
    public VolumeSampleProvider GainSampler { get; }

    public CancellationTokenSource _CancellationToken;

    public void Dispose()
    {
        _CancellationToken.Dispose();
        Reder.Dispose();
        WaveOut.Dispose();
    }
}

public class NAudioPlayer : IAudioPlayer
{
    private ConcurrentDictionary<string, VorbisHolder> _holders = new ConcurrentDictionary<string, VorbisHolder>();
    private string _currentPlayback;
    public ValueTask Pause(string playbackId, bool releaseResources)
    
    
    {
        if (_holders.TryGetValue(playbackId, out var item))
        {
            item.WaveOut.Pause();
        }

        return new ValueTask();
    }
    public ValueTask Resume(string playbackId)
    {
        if (_holders.TryGetValue(playbackId, out var item))
        {
            item.WaveOut.Play();
        }

        return new ValueTask();
    }
    public void Gain(string playbackId, float getGain)
    {
        if (_holders.TryGetValue(playbackId, out var state))
        {
            state.GainSampler.Volume = getGain;
        }
    }

    public event EventHandler<(string PlaybackId, PlaybackStateType)>? StateChanged;
    public void SetVolume(string playbackid, float volume)
    {
        if(_holders.TryGetValue(playbackid, out var state))
        {
            state.WaveOut.Volume = volume;
        }
    }

    public async ValueTask InitStream(SuperAudioFormat codec,
        AbsChunkedInputStream audioStreamStream,
        float normalizationFactor,
        int duration, string playbackId, long playFrom)
    {
        try
        {
            var vorbisStream = new VorbisWaveReader(audioStreamStream);
            // var waveOut = new WaveOutEvent();
            var waveOut = new WaveOutEvent();
            var volumeSampler = new VolumeSampleProvider(vorbisStream.ToSampleProvider())
            {
                Volume = 0
            };
            var newHolder = new VorbisHolder(vorbisStream, waveOut, volumeSampler);
            _holders[playbackId] = newHolder;
            waveOut.Init(volumeSampler);
            if (playFrom > 0)
                vorbisStream.CurrentTime = TimeSpan.FromMilliseconds(playFrom);
            
            waveOut.Play();

            // wait here until playback stops or should stop
            Task.Run(async () =>
            {
                try
                {
                    while (waveOut.PlaybackState != PlaybackState.Stopped &&
                           !newHolder._CancellationToken.IsCancellationRequested
                           &&vorbisStream.Length > vorbisStream.Position)
                    {
                        try
                        {
                            await Task.Delay(100, newHolder._CancellationToken.Token);
                            TimeChanged?.Invoke(this, (playbackId, (int) vorbisStream.CurrentTime.TotalMilliseconds));
                        }
                        catch (Exception x)
                        {
                            S_Log.Instance.LogError(x);
                            if (newHolder._CancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                        }
                    }
                }
                finally
                {

                    TrackFinished?.Invoke(this, playbackId);
                }
            });
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
        }
    }

    public int Time(string playbackId)
    {
        if (_holders.TryGetValue(playbackId, out var item))
        {
            return (int) item.Reder.CurrentTime.TotalMilliseconds;
        }

        return -1;
    }

    public void Dispose(string playbackId)
    {
        if (_holders.TryRemove(playbackId, out var item))
        {          
            item._CancellationToken.Cancel();
            item.Dispose();
        }
    }

    public void Seek(string playbackId, int posInMs)
    {
        if (_holders.TryGetValue(playbackId, out var item))
        {
            item.Reder.CurrentTime = TimeSpan.FromMilliseconds(posInMs);
        }
    }

    public event EventHandler<(string playbackId, int Time)>? TimeChanged;
    public event EventHandler<string>? TrackFinished;
    
}