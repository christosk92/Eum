using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Logging;
using LibVLCSharp;
using LibVLCSharp.Shared;
using Nito.AsyncEx;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Eum.Connections.Spotify.VLC;


public class VorbisHolder
{
    public VorbisHolder(Media media, MediaPlayer player, StreamMediaInput mediaInput, AbsChunkedInputStream stream)
    {
        Media = media;
        Player = player;
        MediaInput = mediaInput;
        Stream = stream;
        _CancellationToken = new CancellationTokenSource();
    }

    public Media Media { get; }
    public StreamMediaInput MediaInput { get; }
    public MediaPlayer Player { get; }

    public AbsChunkedInputStream Stream { get; }
    public float PreviousGain { get; set; }

    public CancellationTokenSource _CancellationToken;
}

public class EumVlcPlayer : IAudioPlayer
{
    private ConcurrentDictionary<string, VorbisHolder> _holders = new ConcurrentDictionary<string, VorbisHolder>();
    private string _currentPlayback;
    private readonly LibVLC _libVlc;

    public EumVlcPlayer()
    {
        _libVlc = new LibVLC(enableDebugLogs: true);

    }

    public void ReleaseAll()
    {
        foreach (var kvp in _holders)
        {
            Dispose(kvp.Key);
        }
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
            //TODO:
            // if (Math.Abs(getGain - state.PreviousGain) > 0.001)
            // {
            //     //check if decreased or increased
            //     if (getGain - state.PreviousGain > 0)
            //     {
            //         //increase 
            //         //so new track
            //         var equalizer = new Equalizer();
            //         equalizer.SetPreamp(-20 * (1 - getGain));
            //         state.Player.SetEqualizer(equalizer);
            //     }
            //     else
            //     {
            //         var equalizer = new Equalizer();
            //         equalizer.SetPreamp(-20 * getGain);
            //         state.Player.SetEqualizer(equalizer);
            //     }
            //
            //     state.PreviousGain = getGain;
            // }
        }
    }

    public event EventHandler<(string PlaybackId, PlaybackStateType)>? StateChanged;
    public void SetVolume(string playbackid, float volume)
    {
        if (_holders.TryGetValue(playbackid, out var state))
        {
           // state.Player.Volume = 
            //state.Player.SetVolume((int)(volume * 100));
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
            await media.Parse(MediaParseOptions.FetchNetwork);
            foreach (var mediaTrack in media.Tracks)
            {
                
            }
            var player = new MediaPlayer(media);
            player.EnableHardwareDecoding = true;
            // var waveOut = new WaveOutEvent();
            var newHolder = new VorbisHolder(media, player, streammediaInput, audioStreamStream);
            _holders[playbackId] = newHolder;
            async void playing(object? sender, EventArgs eventArgs)
            {
                try
                {
                    if (playFrom > 0)
                    {
                        await Task.Delay(20);
                        var isplaying = player.IsPlaying;
                        var l =
                            player.Length;
                        //player.SetTime(playFrom, true);
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
                player.Playing -= playing;
                player.EndReached -= stopped_ended;
                player.Stopped -= stopped_ended;
                player.EncounteredError -= error;
                player.TimeChanged -= timechanged;
                TrackFinished?.Invoke(this, playbackId);
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

            _ = Task.Run(async () =>
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

    public ValueTask<int> Time(string playbackId)
    {
        if (_holders.TryGetValue(playbackId, out var item))
        {
            return new ValueTask<int>((int)item.Player.Time);
        }

        return new ValueTask<int>(-1);
    }

    public async void Dispose(string playbackId)
    {
        if (_holders.TryRemove(playbackId, out var item))
        {
            try
            {
                await Task.Run(() =>
                {
                    item.Player.Stop();
                });
                item.Stream.Dispose();
                item.MediaInput.Dispose();
                item.Media.Dispose();
                item.Player.Dispose();

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
            var l =
                item.Player.Length;
            item.Player.Time = posInMs;
            //var a =
            // item.Player.SetTime(posInMs, true);
        }
    }

    public event EventHandler<(string playbackId, int Time)>? TimeChanged;
    public event EventHandler<string>? TrackFinished;

}