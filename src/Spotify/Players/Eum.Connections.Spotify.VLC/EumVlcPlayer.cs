using System.Collections.Concurrent;
using System.Diagnostics;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Logging;
using LibVLCSharp.Shared;
using Nito.AsyncEx;


namespace Eum.Connections.Spotify.VLC;


public class VorbisHolder
{
    public VorbisHolder(Media media, MediaPlayer player, StreamMediaInput mediaInput)
    {
        Media = media;
        Player = player;
        MediaInput = mediaInput;
        _CancellationToken = new CancellationTokenSource();
    }

    public Media Media { get; }
    public StreamMediaInput MediaInput { get; }
    public MediaPlayer Player { get; }

    public CancellationTokenSource _CancellationToken;
}

public class EumVlcPlayer : IAudioPlayer
{
    private ConcurrentDictionary<string, VorbisHolder> _holders = new ConcurrentDictionary<string, VorbisHolder>();
    private string _currentPlayback;
    private readonly LibVLC _libVlc;

    public EumVlcPlayer()
    {
        _libVlc = new LibVLC(enableDebugLogs: false);
    }
    public ValueTask Pause(string playbackId, bool releaseResources)
    {
        if (_holders.TryGetValue(playbackId, out var item))
        {
            item.Player.Pause();
        }

        return new ValueTask();
    }
    public ValueTask Resume(string playbackId)
    {
        if (_holders.TryGetValue(playbackId, out var item))
        {
            item.Player.Play();
        }

        return new ValueTask();
    }

    public void Gain(string playbackId, float getGain)
    {
        if (_holders.TryGetValue(playbackId, out var state))
        {
            //I want to crossfade the audio here, that means i need to change the gain of the current playback
            //we need to use the state.media.addoption
            
           // state.Media.AddOption($":audio-filter=volume:gain={getGain}");
        }
    }

    public event EventHandler<(string PlaybackId, PlaybackStateType)>? StateChanged;
    public void SetVolume(string playbackid, float volume)
    {
        if(_holders.TryGetValue(playbackid, out var state))
        {
            state.Player.Volume = (int)(volume * 100);
        }
    }

    public async ValueTask InitStream(SuperAudioFormat codec,
        AbsChunkedInputStream audioStreamStream,
        float normalizationFactor,
        int duration, string playbackId, 
        long playFrom)
    {
        try
        {
            var streammediaInput = new StreamMediaInput(audioStreamStream);
            var media = new Media(_libVlc, streammediaInput);
            var player = new MediaPlayer(media);
            // var waveOut = new WaveOutEvent();
            var newHolder = new VorbisHolder(media, player, streammediaInput);
            _holders[playbackId] = newHolder;
            
            async void playing(object? sender, EventArgs eventArgs)
            {
                try
                {
                    if (playFrom > 0)
                    {
                        await Task.Delay(20);
                        player.Time = playFrom;
                        StateChanged?.Invoke(this, (playbackId, PlaybackStateType.Resumed));
                    }
                }
                catch (Exception x)
                {
                    
                }
            }
            var waitForEnd = new AsyncManualResetEvent(false);

            void stopped_ended(object? sender, EventArgs eventArgs)
            {
                TrackFinished?.Invoke(this, playbackId);
                
                player.Playing -= playing;
                player.Stopped -= stopped_ended;
                player.EncounteredError -= error;
                player.EndReached -= stopped_ended;
                player.TimeChanged -= timechanged;
                waitForEnd.Set();
            }
            void error(object? sender, EventArgs eventArgs)
            {
                Debugger.Break();
            }
            
            void timechanged(object? sender, EventArgs eventArgs)
            {
                TimeChanged?.Invoke(this, (playbackId, (int)player.Time));
            }
            player.Playing += playing;
            player.Stopped += stopped_ended;
            player.EncounteredError += error;
            player.EndReached += stopped_ended;
            player.TimeChanged += timechanged;
            player.Play();

            _ = Task.Run(async() =>
            {
                try
                {
                    await waitForEnd.WaitAsync(newHolder._CancellationToken.Token);
                }
                finally
                {
                    if (!newHolder._CancellationToken.IsCancellationRequested)
                        stopped_ended(this, null);
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
            return (int) item.Player.Time;
        }

        return -1;
    }

    public void Dispose(string playbackId)
    {
        if (_holders.TryRemove(playbackId, out var item))
        {
            try
            {
                item.MediaInput.Dispose();
                item.Player.Dispose();
                item.Media.Dispose();
                item._CancellationToken.Cancel();
                item._CancellationToken.Dispose();
            }
            catch (Exception x)
            {
                
            }
        }
    }

    public void Seek(string playbackId, int posInMs)
    {
        if (_holders.TryGetValue(playbackId, out var item))
        {
            item.Player.Time = posInMs;
        }
    }

    public event EventHandler<(string playbackId, int Time)>? TimeChanged;
    public event EventHandler<string>? TrackFinished;
    
}