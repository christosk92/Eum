using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Eum.Connections.Spotify.Events;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.Audio;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Exceptions;
using Eum.Connections.Spotify.Playback.Metrics;
using Eum.Connections.Spotify.Playback.Playback;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Connections.Spotify.Playback.States;
using Eum.Connections.Spotify.Playback.Transitions;
using Eum.Logging;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.transfer;

namespace Eum.Connections.Spotify.Playback;

/// <summary>
/// Playback on a local device
/// </summary>
public class SpotifyPlaybackClient : ISpotifyPlaybackClient, IDeviceStateHandlerListener
{
    public const int VOLUME_MAX = 65536;

    private readonly IAudioPlayer? _sink;
    private readonly ISpotifyClient _spotifyClient;
    private readonly EventsDispatcher _events;
    private PlayerSession? _playerSession;

    private readonly ConcurrentDictionary<string?, PlaybackMetrics> _metrics = new();

    public SpotifyPlaybackClient(ISpotifyClient spotifyClient, IAudioPlayer? sink)
    {
        _spotifyClient = spotifyClient;
        _sink = sink;
        _events = new EventsDispatcher(spotifyClient.Config, this);
        //TODO: make interface/testable
        InitState();
        State.ClusterChanged += (sender, update) => ClusterChanged?.Invoke(this, update);
        _sink.StateChanged += async (sender, tuple) =>
        {
            if (tuple.PlaybackId == State.State.PlaybackId)
            {
                switch (tuple.Item2)
                {
                    case PlaybackStateType.Resumed:
                        State.SetState(true, false, false);
                        await State.Updated();
                        break;
                    case PlaybackStateType.Paused:
                        break;
                    case PlaybackStateType.Seeked:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        };
    }

    public void AddEventsListener(IEventsListener listener)
    {
        _events._listeners.Add(listener);
    }

    public void RemoveEventsListener(IEventsListener listener)
    {
        _events._listeners.Remove(listener);
    }

    private void InitState()
    {
        State = new StateWrapper(_spotifyClient, this, _sink != null);
        State.AddListener(this);
    }

    public Cluster LatestCluster => State.LatestCluster;
    public event EventHandler<ClusterUpdate>? ClusterChanged;

    public async Task PlayOnDevice(SpotifyId contextId, SpotifyId? trackUri = null, int? trackIndex = null, string? deviceId = null,
        CancellationToken ct = default)
    {
        if (deviceId == null)
        {
            //play on local device
            var skipTo = new object();
            if (trackUri != null && trackIndex != null)
            {
                skipTo = new
                {
                    track_uri = trackUri.Value.Uri,
                    track_index = trackIndex.Value
                };
            }
            else if (trackIndex != null && trackUri == null)
            {
                skipTo = new
                {
                    track_index = trackIndex.Value
                };
            }
            else if (trackIndex == null && trackUri != null)
            {
                skipTo = new
                {
                    track_uri = trackUri.Value.Uri,
                };
            }
            else
            {
                throw new NotSupportedException();
            }

            var data = new
            {
                endpoint = "play",
                context = new
                {
                    uri = contextId.Uri,
                    url = $"context://{contextId.Uri}",
                    restrictions = new object()
                },
                play_origin = new
                {
                    feature_identifier = contextId.Type.ToString().ToLower(),
                    feature_version = "xpui_2022-12-05_1670256197369_abf054a",
                    referrer_identifier = "now_playing_bar",
                    device_identifier = _spotifyClient.Config.DeviceId
                },

                options = new
                {
                    always_play_something = false,
                    skip_to = skipTo,
                    initially_paused = false,
                    system_initiated = false,
                    player_options_override = new object(),
                    suppressions = new object(),
                    prefetch_level = "none",
                    audio_stream = "default",
                    session_id = string.Empty,
                    license = "premium"
                },
                play_options = new
                {
                    override_restrictions = false,
                    only_for_local_device = false,
                    system_initiated = false,
                    reason = "interactive",
                    operation = "replace",
                    trigger = "immediately"
                }
            };
            await Task.Run(async () => await HandlePlay(JsonSerializer.SerializeToElement(data)), ct);
        }
    }


    /// <summary>
    /// Enter a "panic" state where everything is stopped.
    /// </summary>
    /// <param name="reason">Reason why we entered this mode.</param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task PanicState(PlaybackMetricsReason? reason)
    {
        await _sink.Pause(State.State.PlaybackId, true);
        State.SetState(false, false, false);
        await State.Updated();

        _events.PanicState();
        if (reason == null)
        {
            _metrics.Clear();
        }
        else if (_playerSession != null)
        {
            EndMetrics(_playerSession.CurrentPlaybackId, reason ?? PlaybackMetricsReason.END_PLAY,
                _playerSession.CurrentMetrics, State.GetPosition());
        }

        _events.PanicState();
    }

    public StateWrapper State { get; private set; }
    public MetadataWrapper CurrentMetadata { get; }

    public ValueTask<int> Time
    {
        get
        {
            try
            {
                return _playerSession?.CurrentTime ?? new ValueTask<int>(-1);
            }
            catch (CannotGetTimeException ex)
            {
                return new ValueTask<int>(-1);
            }
        }
    }

    public ValueTask Ready()
    {
        _events.VolumeChanged(State.Volume);
        return new ValueTask();
    }

    public async ValueTask Command(CommandEndpoint endpoint, CommandBody data)
    {
        S_Log.Instance.LogInfo("Received command: " + endpoint);

        switch (endpoint)
        {
            case CommandEndpoint.Play:
                Task.Run(async () => await HandlePlay(data.Object));
                break;
            case CommandEndpoint.Pause:
                Task.Run(async () => await HandlePause());
                break;
            case CommandEndpoint.Resume:
                Task.Run(async () => await HandleResume());
                break;
            case CommandEndpoint.SeekTo:
                var seekTo = data.ValueInt();
                Task.Run(async () => await HandleSeek(seekTo.Value));
                break;
            case CommandEndpoint.SkipNext:
                break;
            case CommandEndpoint.SkipPrev:
                break;
            case CommandEndpoint.SetShufflingContext:
                break;
            case CommandEndpoint.SetRepeatingContext:
                break;
            case CommandEndpoint.SetRepeatingTrack:
                break;
            case CommandEndpoint.UpdateContext:
                break;
            case CommandEndpoint.SetQueue:
                break;
            case CommandEndpoint.AddToQueue:
                break;
            case CommandEndpoint.Transfer:
                Task.Run(async () => await HandleTransferState(TransferState.Parser.ParseFrom(data.Data)));
                break;
            case CommandEndpoint.Error:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null);
        }
    }

    private async Task HandlePlay(JsonElement dataObject)
    {
        var dt = dataObject.ToString();

        S_Log.Instance.LogInfo($"Loading context (play), uri: {PlayCommandHelper.GetContextUri(ref dataObject)}");

        try
        {
            var sessionId = await State.Load(dataObject);
            _events.ContextChanged();

            var paused = PlayCommandHelper.IsPaused(ref dataObject, false);
            await LoadSession(sessionId, !paused, PlayCommandHelper.WillSkipToSomething(ref dataObject), false);
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError("Failed loading context!", x);
            await PanicState(null);
        }
    }

    private async Task HandleTransferState(TransferState cmd)
    {
        S_Log.Instance.LogInfo($"Loading context (transfer), uri: {cmd.CurrentSession.Context.Uri}");

        try
        {
            var sw = Stopwatch.StartNew();
            var sessionId = await State.Transfer(cmd);
            _events.ContextChanged();
            await LoadSession(sessionId, !cmd.Playback.IsPaused, true);
            sw.Stop();
            S_Log.Instance.LogInfo($"Finished transfer state, took {sw.Elapsed}");
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
            await PanicState(null);
        }
    }

    public ValueTask VolumeChanged()
    {
        var volume = State.Volume;

        var volumeNorm = ((float)volume / VOLUME_MAX);
        if (!_spotifyClient.Config.BypassSinkVolume)
            _sink.SetVolume(State.State.PlaybackId, volumeNorm);
        _events.VolumeChanged(State.Volume);
        return new ValueTask();
    }

    public async ValueTask NotActive()
    {
        _events.InactiveSession(false);
        await _sink.Pause(State.State.PlaybackId, true);
    }

    private void StartMetrics(string playbackId, PlaybackMetricsReason reason, int pos)
    {
        var pm = new PlaybackMetrics(State.CurrentPlayable.Value, playbackId, State, _spotifyClient.TimeProvider);
        pm.StartedHow(reason, State.State.PlayOrigin.FeatureIdentifier);
        pm.StartInterval(pos);
        _metrics[playbackId] = pm;
    }

    private void EndMetrics(string? playbackId, PlaybackMetricsReason reason, PlayerMetrics? metrics, long position)
    {
        if (playbackId == null) return;

        if (!_metrics.TryRemove(playbackId, out var metric))
        {
            return;
        }

        metric.EndedHow(reason, State.State.PlayOrigin.FeatureIdentifier);
        metric.EndInterval(position);
        metric.Update(metrics);
        metric.SendEvents(_spotifyClient, State.Device);
    }

    private class EventsDispatcher
    {
        internal readonly List<IEventsListener> _listeners = new();
        private readonly ISpotifyPlaybackClient _player;

        public EventsDispatcher(SpotifyConfig spotifyClientConfig, ISpotifyPlaybackClient player)
        {
            _player = player;
        }


        public void PlaybackEnded()
        {
            foreach (var l in _listeners)
            {
                Task.Run(() => l.OnPlaybackEnded(_player));
            }
        }

        public void PlaybackPaused()
        {
            long trackTime = _player.State.GetPosition();
            foreach (var l in _listeners)
            {
                Task.Run(() => l.OnPlaybackPaused(_player, trackTime));
            }
        }

        public void PlaybackResumed()
        {
            long trackTime = _player.State.GetPosition();
            foreach (var l in _listeners)
            {
                Task.Run(() => l.OnPlaybackResumed(_player, trackTime));
            }
        }

        public void ContextChanged()
        {
            var uri = _player.State.ContextUri;
            if (uri == null) return;

            foreach (var l in _listeners)
            {
                Task.Run(() => l.OnContextChanged(_player, uri));
            }
        }

        public void TrackChanged(bool userInitiated)
        {
            var id = _player.State.CurrentPlayable;
            if (id == null) return;

            MetadataWrapper metadata = _player.CurrentMetadata;
            foreach (var l in _listeners)
            {
                Task.Run(() => l.OnTrackChanged(_player, id.Value, metadata, userInitiated));
            }
        }

        public void Seeked(int pos)
        {
            foreach (var listener in _listeners)
            {
                Task.Run(() => listener.OnTrackSeeked(_player, pos));
            }
        }

        public void VolumeChanged(uint value)
        {
            if (value > VOLUME_MAX) throw new ArgumentOutOfRangeException();
            float volume = (float)value / VOLUME_MAX;

            foreach (var listener in _listeners)
            {
                Task.Run(() => listener.OnVolumeChanged(_player, volume));
            }
        }

        public void MetadataAvailable()
        {
            MetadataWrapper metadata = _player.CurrentMetadata;
            if (metadata == null) return;

            foreach (var eventsListener in _listeners)
            {
                Task.Run(() => eventsListener.OnMetadataAvailable(_player, metadata));
            }
        }

        public void PlaybackHaltStateChanged(bool halted)
        {
            long trackTime = _player.State.GetPosition();
            foreach (var eventsListener in _listeners)
            {
                Task.Run(() => eventsListener.OnPlaybackHaltStateChanged(_player, halted, trackTime));
            }
        }

        public void InactiveSession(bool timeout)
        {
            foreach (var eventsListener in _listeners)
            {
                Task.Run(() => eventsListener.OnInactiveSession(_player, timeout));
            }
        }

        internal void PanicState()
        {
            foreach (var l in _listeners)
            {
                Task.Run(() => l.OnPanicState(_player));
            }
        }

        public void PlaybackFailed(Exception exception)
        {
            foreach (var l in _listeners)
            {
                Task.Run(() => l.OnPlaybackFailed(_player, exception));
            }
        }
    }

    public async Task TestPlayback()
    {
        await Load("spotify:track:5xrtzzzikpG3BLbo4q1Yul", true, false);
        // var session = new PlayerSession(_spotifyClient, _mediaPlayer, "test", new PlayerSessionListener());
        //
        // //https://open.spotify.com/track/73OZT1XgLleDwperqGjWH4?si=455c43549b1c440f
        // await session.Play(new SpotifyId("spotify:track:73OZT1XgLleDwperqGjWH4"), 0, PlaybackMetricsReason.APP_LOAD);
    }

    public async Task Load(string uri, bool play, bool shuffle)
    {
        try
        {
            var sessionId = await State.LoadContext(uri);
            //events.contextChanged();

            State.SetShufflingContext(shuffle);
            await LoadSession(sessionId, play, true, false);
        }
        catch (Exception ex)
        {
            S_Log.Instance.LogError("Cannot play context!", ex);
            PanicState(null);
        }
    }

    // public async Task Load(string contextUri, int index, bool play, bool shuffle)
    // {
    //     try
    //     {
    //         var sessionId = await State.LoadContext(contextUri);
    //         //events.contextChanged();
    //
    //         State.SetShufflingContext(shuffle);
    //         await LoadSession(sessionId, play, true);
    //     }
    //     catch (Exception ex)
    //     {
    //         S_Log.Instance.LogError("Cannot play context!", ex);
    //         PanicState(null);
    //     }
    // }
    private async Task LoadSession(string sessionId, bool play, bool withSkip,
        bool getPosition = true)
    {
        S_Log.Instance.LogInfo($"Loading session, id: {sessionId}, play: {play}");

        TransitionInfo trans = TransitionInfo.ContextChange(State, withSkip);

        if (_playerSession != null)
        {
            EndMetrics(_playerSession.CurrentPlaybackId, trans.EndedReason, _playerSession.CurrentMetrics,
                trans.EndedWhen);
            _playerSession?.Dispose();
            _playerSession = null;
        }

        _playerSession =
            new PlayerSession(_spotifyClient, _sink, sessionId, new PlayerSessionListener(State, this));
        await Task.Run(async () =>
        {
            try
            {
                await _spotifyClient.EventService.SendEvent(
                    new NewSessionIdEvent(sessionId, State, _spotifyClient.TimeProvider)
                        .Build());
            }
            catch (Exception ex)
            {

            }
        });

        await LoadTrack(play, trans, getPosition);
    }
    //System.ArgumentException: Value does not fall within the expected range.

    private async Task LoadTrack(bool play, TransitionInfo trans, bool getPosition = true)
    {
        if (_playerSession?.CurrentPlaybackId is not null)
            EndMetrics(_playerSession.CurrentPlaybackId, trans.EndedReason, _playerSession.CurrentMetrics,
                trans.EndedWhen);
        S_Log.Instance.LogInfo(
            $"Loading track, id: {State.CurrentPlayable.Value.Uri}, session: {_playerSession.SessionId}, play: {play}");

        var playbackId = await _playerSession.Play(State.CurrentPlayable.Value, getPosition ? (int)State.GetPosition() : 0,
            play,
            trans.StartedReason);
        State.State.PlaybackId = playbackId;
        if (play) await _sink.Resume(playbackId);
        else await _sink.Pause(playbackId, false);

        State.SetState(true, !play, true);
        await State.Updated();

        _events.TrackChanged(true);
        if (play) _events.PlaybackResumed();
        else _events.PlaybackPaused();

        StartMetrics(playbackId, trans.StartedReason, (int)State.GetPosition());
        await _spotifyClient.EventService.SendEvent(
            new NewPlaybackIdEvent(_playerSession.SessionId, playbackId, _spotifyClient.TimeProvider).Build());
    }

    private async Task HandleSeek(int mediaPlayerTime)
    {
        await _playerSession.SeekCurrent(mediaPlayerTime);
        State.SetPosition(mediaPlayerTime);
        await State.Updated();
        _events.Seeked(mediaPlayerTime);
        if (_playerSession is not null)
        {
            if (_metrics.TryGetValue(_playerSession.CurrentPlaybackId, out var pm))
            {
                pm.EndInterval(State.GetPosition());
                pm.StartInterval(mediaPlayerTime);
            }
        }
    }

    private async Task HandlePause()
    {
        if (!State.IsPaused)
        {
            State.SetState(true, true, false);
            await _sink.Pause(State.State.PlaybackId, false);

            if (_playerSession != null)
                State.SetPosition(await _playerSession.CurrentTime);


            await State.Updated();
            _events.PlaybackPaused();
        }
    }

    private async Task HandleResume()
    {
        if (State.IsPaused)
        {
            State.SetState(true, false, false);
            await _sink.Resume(State.State.PlaybackId);

            await State.Updated();
            _events.PlaybackResumed();
        }
    }

    private class PlayerSessionListener : IPlayerSessionListener
    {
        private SpotifyPlaybackClient _events;
        private StateWrapper _state;

        public PlayerSessionListener(StateWrapper state, SpotifyPlaybackClient events)
        {
            _state = state;
            _events = events;
        }

        public SpotifyId CurrentPlayable() => _state.CurrentPlayable.Value;

        public async ValueTask<SpotifyId?> NextPlayable()
        {
            var next = await _state.NextPlayable(_events._spotifyClient.Config.AutoplayEnabled);
            if (next == NextPlayableType.AUTOPLAY)
            {
                await _events.LoadAutoplay();
                return null;
            }

            if (next is NextPlayableType.OK_PLAY or NextPlayableType.OK_PAUSE or NextPlayableType.OK_REPEAT)
            {
                if (next != NextPlayableType.OK_PLAY && next != NextPlayableType.OK_REPEAT)
                    await _events._sink.Pause(_state.State.PlaybackId, false);

                return _state.CurrentPlayable.Value;
            }
            else
            {
                S_Log.Instance.LogError("Failed loading next song: " + next);
                await _events.PanicState(PlaybackMetricsReason.END_PLAY);
                return null;
            }
        }

        public async ValueTask<SpotifyId?> NextPlayableDoNotSet()
        {
            return await _state.NextPlayableDoNotSet();
        }

        public IReadOnlyDictionary<string, string>? MetadataFor(SpotifyId id)
        {
            var current = GetCurrentTrack();
            if (current != null && id.Matches(current))
                return current.Metadata;

            var index = _state.Tracks.Tracks.FindIndex(a => id.Matches(a));
            if (index == -1)
            {
                index = _state.Tracks.Queue.FindIndex(a => id.Matches(a));
                if (index == -1)
                    return null;
            }

            return _state.Tracks.Tracks[index].Metadata;
        }

        public ContextTrack GetCurrentTrack()
        {
            int index = (int)_state.State.Index.Track;
            return _state.Tracks == null
                   || _state.Tracks.Tracks.Count < index
                ? null
                : _state.Tracks.Tracks[index];
        }

        public async void PlaybackHalted(int chunk)
        {
            S_Log.Instance.LogInfo($"Playback halted on retrieving chunk {chunk}.");
            _state.SetIsBuffering(true);
            await _state.Updated();

            _events._events.PlaybackHaltStateChanged(true);
        }

        public async Task PlaybackResumedFromHalt(int chunk, long diff)
        {
            S_Log.Instance.LogInfo($"Playback resumed, chunk {chunk} retrieved, took {diff}ms.");
            _state.SetPosition(Math.Max(0, _state.GetPosition() - diff));
            _state.SetIsBuffering(false);
            await _state.Updated();

            _events._events.PlaybackHaltStateChanged(false);
        }

        public async void StartedLoading()
        {
            if (!(_state.State.IsPaused && _state.State.IsPlaying))
            {
                _state.SetIsBuffering(true);
                await _state.Updated();
            }
        }

        public async void LoadingError(Exception ex)
        {
            _events._events.PlaybackFailed(ex);
            if (ex is ContentRestrictedException)
            {
                S_Log.Instance.LogError("Can't load track (content restricted).", ex);
            }
            else
            {
                S_Log.Instance.LogError("Failed loading track.", ex);
                await _events.PanicState(PlaybackMetricsReason.TRACK_ERROR);
            }
        }

        public async void FinishedLoading(MetadataWrapper metadataWrapper)
        {
            _state.EnrichWithMetadata(metadataWrapper);
            _state.SetIsBuffering(false);
            await _state.Updated();
        }

        public async void PlaybackError(Exception ex)
        {
            if (ex is ChunkException)
                S_Log.Instance.LogError("Failed retrieving chunk, playback failed!", ex);
            else
                S_Log.Instance.LogError("Playback error!", ex);

            await _events.PanicState(PlaybackMetricsReason.TRACK_ERROR);
        }

        public async void TrackChanged(string playbackId, MetadataWrapper metadata, int pos,
            PlaybackMetricsReason reason)
        {
            if (metadata != null) _state.EnrichWithMetadata(metadata);
            _state.State.PlaybackId = playbackId;
            _state.SetPosition(pos);
            await _state.Updated();

            _events._events.TrackChanged(false);
            _events._events.MetadataAvailable();

            await _state.SpotifyClient.EventService.SendEvent(
                new NewPlaybackIdEvent(_state.State.SessionId, playbackId, _state.SpotifyClient.TimeProvider).Build());
            _events.StartMetrics(playbackId, reason, pos);
        }

        public void TrackPlayed(string playbackId, PlaybackMetricsReason reason, PlayerMetrics metrics, int endedt)
        {
            _events.EndMetrics(playbackId, reason, metrics, endedt);
            _events._events.PlaybackEnded();
        }

        public void Dispose()
        {
            _state = null;
            _events = null;
        }
    }

    private async Task LoadAutoplay()
    {
        var context = State.ContextUri;
        if (context == null)
        {
            S_Log.Instance.LogError("Cannot load autoplay with null context!");
            await PanicState(null);
            return;
        }

        var contextDesc = State.State.ContextMetadata["context_description"];

        try
        {
            var resp = await _spotifyClient.MercuryClient.SendAndReceiveResponseAsync(
                $"hm://autoplay-enabled/query?uri={context}");
            switch (resp.StatusCode)
            {
                case 200:
                    Debugger.Break();
                    // var newContext = Encoding.UTF8.GetString(resp.Payload);
                    // var sessionId = state.loadContext(newContext);
                    // state.setContextMetadata("context_description", contextDesc);
                    //
                    // events.contextChanged();
                    // loadSession(sessionId, true, false);
                    //
                    // LOGGER.debug("Loading context for autoplay, uri: {}", newContext);
                    break;
                case 204:
                    break;
                default:
                    break;
            }
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
        }

        throw new NotImplementedException();
    }

    public async void SinkError(Exception ex)
    {
        S_Log.Instance.LogError("Sink error!", ex);
        await PanicState(PlaybackMetricsReason.TRACK_ERROR);
    }
}

public interface IEventsListener
{
    void OnContextChanged(ISpotifyPlaybackClient player, string newUri);

    void OnTrackChanged(ISpotifyPlaybackClient player, SpotifyId id,
        MetadataWrapper metadata,
        bool userInitiated);

    void OnPlaybackEnded(ISpotifyPlaybackClient player);

    void OnPlaybackPaused(ISpotifyPlaybackClient player, long trackTime);

    void OnPlaybackResumed(ISpotifyPlaybackClient player, long trackTime);

    void OnTrackSeeked(ISpotifyPlaybackClient player, long trackTime);

    void OnMetadataAvailable(ISpotifyPlaybackClient player, MetadataWrapper metadata);

    void OnPlaybackHaltStateChanged(ISpotifyPlaybackClient player, bool halted, long trackTime);

    void OnInactiveSession(ISpotifyPlaybackClient player, bool timeout);

    void OnVolumeChanged(ISpotifyPlaybackClient player, float volume);

    void OnPanicState(ISpotifyPlaybackClient player);
    void OnPlaybackFailed(ISpotifyPlaybackClient player, Exception e);
}