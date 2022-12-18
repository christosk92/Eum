using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Enums;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.Audio;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Crossfade;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Connections.Spotify.Playback.Exceptions;
using Eum.Connections.Spotify.Playback.Metrics;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Connections.Spotify.Playback.States;
using Eum.Enums;
using Eum.Logging;
using Eum.Spotify.metadata;
using Nito.AsyncEx;
using Exception = System.Exception;

namespace Eum.Connections.Spotify.Playback.Playback;

public class PlayerQueueEntry : AbsQueueEntry, IHaltListener, IDisposable
{
    public const int INSTANT_PRELOAD = 1;
    public const int INSTANT_START_NEXT = 2;
    public const int INSTANT_END = 3;

    private readonly IAudioPlayer _sink;
    private ISpotifyClient _spotifyClient;
    private readonly bool _preloaded;
    private IPlayerQueueEntryListener _listener;

    private IDecodedAudioStream _audioStream;
    private MetadataWrapper _metadata;
    private bool _closed = false;
    private long playbackHaltedAt = 0;
    private bool _retried = false;
    private PlayableContentMetrics contentMetrics;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly SortedDictionary<int, int> _notifyInstatns =
        new SortedDictionary<int, int>(Comparer<int>.Create((i, i1) => i.CompareTo(i1)));

    private bool _play;
    private long _playFrom;

    public PlayerQueueEntry(IAudioPlayer sink, ISpotifyClient client, SpotifyId id, bool preloaded,
        IPlayerQueueEntryListener listener, bool play,
        long playFrom)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _sink = sink;
        _spotifyClient = client;
        Id = id;
        PlaybackId = StateWrapper.GeneratePlaybackId();
        _preloaded = preloaded;
        _listener = listener;
        _play = play;
        _playFrom = playFrom;
    }


    public string PlaybackId { get; }

    public CrossfadeController CrossfadeController { get; private set; }
    public PlaybackMetricsReason EndReason { get; set; } = PlaybackMetricsReason.END_PLAY;
    public SpotifyId Id { get; }

    public PlayerQueueEntry RetrySelf(bool preloaded)
    {
        if (_retried) throw new ArgumentException();

        var entry = new PlayerQueueEntry(_sink, _spotifyClient, Id, preloaded, _listener, _play, _playFrom);
        entry._retried = true;
        return entry;
    }


    private async Task Load(bool preloaded, CancellationToken ct = default)
    {
        LoadedStream stream = default;

        if (Id.IsLocalId)
        {
            //TODO
            throw new NotImplementedException();
        }
        else
        {
            try
            {
                stream = await GetStream(Id, _spotifyClient.Config.AudioQuality, preloaded, ct);

                _metadata = stream.Metadata;
                contentMetrics = stream.Metrics;
                _audioStream = stream.Streamer;

                if (_metadata.episode != null)
                {
                    S_Log.Instance.LogInfo(
                        $"Loaded episode. Name: {_metadata.episode.Name}, duration: {_metadata.episode.Duration}, uri: {_metadata.Id.Uri}.");
                }
                else
                {
                    S_Log.Instance.LogInfo(
                        $"Loaded track. Name: {_metadata.track.Name}, artists: {string.Join(", ", _metadata.track.Artist.Select(a => a.Name))} duration: {_metadata.track.Duration}, uri: {_metadata.Id.Uri}.");
                }

                CrossfadeController = new CrossfadeController(PlaybackId, _metadata.Duration
                    , _listener.MetadataFor(Id), _spotifyClient.Config);
                if (CrossfadeController.HasAnyFadeOut || _spotifyClient.Config.PreloadEnabled)
                    NotifyInstant(INSTANT_PRELOAD,
                        (int) (CrossfadeController.FadeOutStartTimeMin() - TimeSpan.FromMilliseconds(20).TotalSeconds));

                _crossfAdeGot.Set();
                float normalizationFactor = 1.0f;
                if (stream.NormalizationData == null || !_spotifyClient.Config.Normalization)
                {
                    normalizationFactor = 1;
                }
                else
                {
                    //TODO: Normalization of data
                }

                await _sink.InitStream(_audioStream.Codec, _audioStream.Stream, normalizationFactor,
                    _metadata.Duration, PlaybackId,
                    _playFrom);

                S_Log.Instance.LogInfo(
                    $"Loaded {stream.Streamer.Codec}, decoder: vorbis. of {_audioStream.Stream.ToString()} , playbackId: {PlaybackId}");
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
            }
        }
    }

    public void StreamReadHalted(int chunk, long time)
    {
        playbackHaltedAt = time;
        _listener.PlaybackHalted(this, chunk);
    }

    public void StreamReadResumed(int chunk, long time)
    {
        if (playbackHaltedAt == 0) return;

        int duration = (int) (time - playbackHaltedAt);
        _listener.PlaybackResumed(this, chunk, duration);
    }


    public async Task Do(CancellationToken cancellationToken)
    {
        Task.Run(() => _listener.StartedLoading(this));

        try
        {
            await Load(_preloaded, cancellationToken);
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x, $"{this} terminated at loading.");
            this.Dispose();
            _listener.LoadingError(this, x, _retried);
            return;
        }

        _listener.FinishedLoading(this, _metadata);


        void TimeChanged(object sender, (string playbackId, int time) e)
        {
            if (e.playbackId != PlaybackId) return;
            if (_notifyInstatns.Any()) CheckInstants(e.time);
            _sink.Gain(PlaybackId, CrossfadeController.GetGain(e.time));
        }

        var waitForFinish = new AsyncManualResetEvent(false);
        void TrackFinished(object sender, string playbackId)
        {
            if (playbackId != PlaybackId) return;
            
            var time2 = _sink.Time(PlaybackId);
            S_Log.Instance.LogInfo($"Player time offset is {_metadata.Duration - time2}, id: {PlaybackId}");
            Dispose();

            _sink.TrackFinished -= TrackFinished;
            _sink.TimeChanged -= TimeChanged;
            waitForFinish.Set();
        }

        _sink.TrackFinished += TrackFinished;
        _sink.TimeChanged += TimeChanged;

       await waitForFinish.WaitAsync(_cancellationTokenSource.Token);
        _listener.PlaybackEnded(this);
        S_Log.Instance.LogInfo($"{this} terminated.");
        _cancellationTokenSource.Dispose();
    }

    private void CheckInstants(int time)
    {
        var kvp = _notifyInstatns.FirstOrDefault();
        var key = kvp.Key;
        var callbackId = kvp.Value;
        if (time >= key)
        {
            _notifyInstatns.Remove(key);

            _listener.InstantReached(this, callbackId, time);
            if (_notifyInstatns.Any()) CheckInstants(time);
        }
    }


    public PlayerMetrics Metrics => new PlayerMetrics(contentMetrics, CrossfadeController, _audioStream);
    public int Time => _sink.Time(PlaybackId);
    public MetadataWrapper Metadata => _metadata;


    private static ConcurrentDictionary<string, Track> _tracksCache = new ConcurrentDictionary<string, Track>();
    private AsyncManualResetEvent _waitForOutput = new AsyncManualResetEvent(false);

    public async Task<LoadedStream> GetStream(SpotifyId uri, AudioQuality quality, bool preload,
        CancellationToken ct = default)
    {
        //Get the track with the file_id
        //get the playback license..
        //play the track
        //notify the connect state that we have started playing on this device.

        switch (uri.Type)
        {
            case EntityType.Track:
                var original =
                    _tracksCache.TryGetValue(uri.HexId(), out var data)
                        ? data
                        : await _spotifyClient.Tracks.MercuryTracks.GetTrack(uri.HexId(), ct);
                _tracksCache[uri.HexId()] = original;
                var feeder = new PlayableContentFeeder(_spotifyClient);
                var track = feeder.PickAlternativeIfNecessary(original);
                if (track == null)
                {
                    var country = _spotifyClient.AuthenticatedUser.CountryCode;
                    ContentRestrictedException.CheckRestrictions(country, original.Restriction.ToList());

                    S_Log.Instance.LogError("Couldn't find playable track: " + uri.Uri);
                    throw new FeederException();
                }

                return
                    await feeder.LoadTrack(track, quality, preload, this, ct);
            default:
                throw new NotSupportedException("Only tracks is currently supported");
        }
    }

    public virtual void Dispose()
    {
        _closed = true;
        _spotifyClient = null;

        try
        {
            _sink?.Dispose(PlaybackId);
            _audioStream?.Dispose();
            _cancellationTokenSource?.Cancel();
            _listener = null;
            _audioStream = null;
        }
        catch (Exception)
        {
            //ignored
        }

    }

    public override string ToString()
    {
        if (_metadata == null) return $"{PlaybackId} is still initializing...";
        return $"PlayerQueueEntry: {PlaybackId}, track: {_metadata.track?.Name ?? _metadata.episode?.Name}.";
    }

    public void NotifyInstant(int callbackId, int when)
    {
        if (_sink != null)
        {
            var time = _sink.Time(PlaybackId);
            if (time >= when)
            {
                _listener.InstantReached(this, callbackId, time);
                return;
            }
        }

        _notifyInstatns[when] = callbackId;
    }

    public void Seek(int pos)
    {
        _sink.Seek(PlaybackId, pos);
    }

    public async Task WaitForCrossFade()
    {
        await _crossfAdeGot.WaitAsync();
    }

    private AsyncManualResetEvent _crossfAdeGot = new(false);
}

public class CannotGetTimeException : Exception
{
}

internal class UnsupportedEncodingException : Exception
{
    public UnsupportedEncodingException(string codec)
    {
        Codec = codec;
    }

    public string Codec { get; }
}

public abstract class AbsQueueEntry
{
    public PlayerQueueEntry? Next { get; internal set; } = null;
    public PlayerQueueEntry? Prev { get; set; } = null;

    public void SetNext(PlayerQueueEntry entry)
    {
        if (Next == null)
        {
            Next = entry;
            entry.Prev = (PlayerQueueEntry) this;
        }
        else
        {
            Next.SetNext(entry);
        }
    }

    public bool Remove(PlayerQueueEntry entry)
    {
        if (Next == null) return false;
        if (Next == entry)
        {
            var tmp = Next;
            Next = tmp.Next;
            tmp.Dispose();
            return true;
        }

        return Next.Remove(entry);
    }

    public bool Swap(PlayerQueueEntry oldEntry, PlayerQueueEntry newEntry)
    {
        if (Next == null) return false;
        if (Next == oldEntry)
        {
            Next = newEntry;
            Next.Prev = oldEntry.Prev;
            Next.Next = oldEntry.Next;
            return true;
        }

        return Next.Swap(oldEntry, newEntry);
    }

    public void Clear()
    {
        if (Prev != null)
        {
            var tmp = Prev;
            Prev = null;
            if (tmp != this) tmp?.Clear();
        }

        ((PlayerQueueEntry) this)?.Dispose();
    }
}