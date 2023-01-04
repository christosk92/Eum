using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.Audio;
using Eum.Connections.Spotify.Playback.Crossfade;
using Eum.Connections.Spotify.Playback.Exceptions;
using Eum.Connections.Spotify.Playback.Metrics;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Logging;

namespace Eum.Connections.Spotify.Playback.Playback;

internal class PlayerSession : IPlayerQueueEntryListener, IDisposable

{
    private ISpotifyClient _spotifyClient;
    private IAudioPlayer _sink;
    private readonly string _sessionId;
    private IPlayerSessionListener _listener;
    private readonly PlayerQueue _queue;

    private int _lastPlayPos = 0;
    private PlaybackMetricsReason? _lastReason = null;

    private bool _closed;

    public PlayerSession(ISpotifyClient spotifyClient, IAudioPlayer sink, string sessionId,
        IPlayerSessionListener listener)
    {
        _spotifyClient = spotifyClient;
        _sink = sink;
        _sessionId = sessionId;
        _listener = listener;

        _queue = new PlayerQueue();
        S_Log.Instance.LogInfo($"Created new session. id: {sessionId}");
    }

    public void PlaybackError(PlayerQueueEntry entry, Exception ex)
    {
        if (entry == _queue.Head) _listener.PlaybackError(ex);
        _queue.Remove(entry);
    }

    public async void PlaybackEnded(PlayerQueueEntry entry)
    {
        _listener.TrackPlayed(entry.PlaybackId, entry.EndReason, entry.Metrics, entry.Time);

        if (entry == _queue.Head)
            await Advance(PlaybackMetricsReason.TRACK_DONE);
    }

    public void PlaybackHalted(PlayerQueueEntry entry, int chunk)
    {
        if (entry == _queue.Head) _listener.PlaybackHalted(chunk);
    }

    public void PlaybackResumed(PlayerQueueEntry entry, int chunk, int diff)
    {
        if (entry == _queue.Head) _listener.PlaybackResumedFromHalt(chunk, diff);
    }

    public void InstantReached(PlayerQueueEntry entry, int callbackId, int exactTime)
    {
        switch (callbackId)
        {
            case PlayerQueueEntry.INSTANT_PRELOAD:
                S_Log.Instance.LogInfo($"Preload instant reached Adding next. Called by {entry}");
                if (entry == _queue.Head) Task.Run(async () => await AddNext());
                break;
            case PlayerQueueEntry.INSTANT_START_NEXT:
                S_Log.Instance.LogInfo($"Start next instant reached. Called by {entry}");
                Task.Run(async () => await Advance(PlaybackMetricsReason.TRACK_DONE));
                break;
            case PlayerQueueEntry.INSTANT_END:
                S_Log.Instance.LogInfo("End instant reached");
                entry.Dispose();
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown callback: " + callbackId);
        }
    }

    public void StartedLoading(PlayerQueueEntry entry)
    {
        S_Log.Instance.LogInfo($"{entry} has started loading.");
        if (entry == _queue.Head)
        {
            _listener.StartedLoading();
        }
    }

    public async void LoadingError(PlayerQueueEntry entry, Exception exception, bool retried)
    {
        if (entry == _queue.Head)
        {
            if (exception is ContentRestrictedException)
            {
                await Advance(PlaybackMetricsReason.TRACK_ERROR);
            }
            else if (!retried)
            {
                var newEntry = entry.RetrySelf(false);
                Task.Run(async () =>
                {
                    _queue.Swap(entry, newEntry);
                    await PlayInternal(newEntry.Id, _lastPlayPos, true,
                        _lastReason == null ? PlaybackMetricsReason.TRACK_ERROR : _lastReason.Value);
                });
                return;
            }

            _listener.LoadingError(exception);
        }
        else if (entry == _queue.Head?.Next)
        {
            if (!(exception is ContentRestrictedException) && !retried)
            {
                var newEntry = entry.RetrySelf(true);
                Task.Run(() => _queue.Swap(entry, newEntry));
                return;
            }
        }

        _queue.Remove(entry);
    }

    public void FinishedLoading(PlayerQueueEntry entry, MetadataWrapper metadata)
    {
        S_Log.Instance.LogInfo($"Finished loading: {entry}");

        if (entry == _queue.Head) _listener.FinishedLoading(metadata);
    }

    public IReadOnlyDictionary<string, string>? MetadataFor(SpotifyId id)
    {
        return _listener.MetadataFor(id);
    }


    public string? CurrentPlaybackId => _queue?.Head?.PlaybackId;
    public PlayerMetrics? CurrentMetrics => _queue?.Head?.Metrics;

    public int CurrentTime
    {
        get
        {
            if (_queue.Head == null) return -1;
            else return _queue.Head.Time;
        }
    }

    public string SessionId => _sessionId;

    public void Dispose()
    {
        _closed = true;
        _queue.Dispose();
        _spotifyClient = null;
        _sink = null;
        _listener.Dispose();
        _listener = null;
    }

    private void Add(SpotifyId id, bool preloaded, bool play, long playFrom)
    {
        var entry = new PlayerQueueEntry(_sink, _spotifyClient, id, preloaded, this, play, playFrom);
        _queue.Add(entry);

        if (_queue.Head?.Next == entry)
        {
            var head = _queue.Head;
            if (head != null && head.CrossfadeController != null)
            {
                var customFade = entry.Id.Uri.Equals(head.CrossfadeController.FadeOutPlayable?.Uri);
                FadeInterval? fadeOut = default;
                if ((fadeOut = head.CrossfadeController.SelectFadeOut(PlaybackMetricsReason.TRACK_DONE, customFade)) !=
                    null)
                {
                    head.NotifyInstant(PlayerQueueEntry.INSTANT_START_NEXT, fadeOut.Start);
                }
            }
        }
    }


    private async ValueTask AddNext()
    {
        var playable = await _listener.NextPlayableDoNotSet();
        if (playable != null)
            Add(playable.Value, true, true, 0);
    }

    private bool AdvanceTo(SpotifyId id)
    {
        do
        {
            var entry = _queue.Head;
            if (entry == null) return false;
            if (entry.Id.Uri.Equals(id.Uri))
            {
                var next = _queue.Head?.Next;
                if (next == null || !next.Id.Uri.Equals(id.Uri))
                    return true;
            }
        } while (_queue.Advance());

        return false;
    }

    private async ValueTask Advance(PlaybackMetricsReason reason)
    {
        if (_closed) return;

        var next = await _listener.NextPlayable();
        if (next == null)
            return;

        var entry = await PlayInternal(next.Value, 0, true, reason);
        _listener.TrackChanged(entry.Entry.PlaybackId, entry.Entry.Metadata, entry.Pos, reason);
    }


    public async Task<string> Play(SpotifyId spotifyId, int i, bool play, PlaybackMetricsReason appLoad)
    {
        var playedResult = await PlayInternal(spotifyId, i, play, appLoad);

        return playedResult.Entry.PlaybackId;
    }

    private async Task<EntryWithPos> PlayInternal(SpotifyId spotifyId, int pos, bool play, PlaybackMetricsReason lastReason)
    {
        _lastPlayPos = pos;
        _lastReason = lastReason;

        if (!AdvanceTo(spotifyId))
        {
            Add(spotifyId, false, play, pos);
            _queue.Advance();
        }

        var head = _queue.Head;


        var customFade = false;
        if (head.Prev != null)
        {
            head.Prev.EndReason = lastReason;
            if (head.Prev.CrossfadeController == null)
            {
                head.Prev.Dispose();
                customFade = false;
            }
            else
            {
                customFade = head.Id.Uri.Equals(head.Prev.CrossfadeController.FadeOutPlayable?.Uri);
                FadeInterval? fadeOut = default;
                await head.Prev.WaitForCrossFade();
                if (head.Prev.CrossfadeController == null
                    || (fadeOut = head.Prev.CrossfadeController.
                        SelectFadeOut(lastReason, customFade)) == null)
                {
                    head.Prev.Dispose();
                }
                else
                {
                    switch (fadeOut)
                    {
                        case PartialFadeInterval:
                        {
                            int time = head.Prev.Time;
                            head.Prev.NotifyInstant(PlayerQueueEntry.INSTANT_END,
                                fadeOut.Start);
                            break;
                        }
                        default:
                            head.Prev.NotifyInstant(PlayerQueueEntry.INSTANT_END, fadeOut.End);
                            break;
                    }
                }
            }
        }


        await head.WaitForCrossFade();
        FadeInterval? fadeIn = default;
        if (head.CrossfadeController != null &&
            (fadeIn = head.CrossfadeController.SelectFadeIn(lastReason, customFade)) != null)
        {
            head.Seek(pos - fadeIn.Start);
        }
        else
        {
            head.Seek(pos);
        }

        S_Log.Instance.LogInfo(
            $"{head} has been added to the output: Session: {_sessionId}, pos: {TimeSpan.FromMilliseconds(pos)}, reason: {lastReason}");
        return new EntryWithPos(head, pos);
    }

    public async Task SeekCurrent(int pos)
    {
        if (_queue.Head == null) return;
        PlayerQueueEntry entry;

        if ((entry = _queue.Head?.Prev) != null)
        {
            _queue.Remove(entry);
        }

        if ((entry = _queue.Head?.Next) != null)
        {
            _queue.Remove(entry);
        }

        _queue.Head.Seek(pos);
    }
}

internal record EntryWithPos(PlayerQueueEntry Entry, int Pos);

internal interface IPlayerSessionListener : IDisposable
{
    SpotifyId CurrentPlayable();
    ValueTask<SpotifyId?> NextPlayable();
    ValueTask<SpotifyId?> NextPlayableDoNotSet();

    IReadOnlyDictionary<string, string>? MetadataFor(SpotifyId id);

    void PlaybackHalted(int chunk);
    Task PlaybackResumedFromHalt(int chunk, long diff);

    void StartedLoading();
    void LoadingError(Exception ex);
    void FinishedLoading(MetadataWrapper metadataWrapper);
    void PlaybackError(Exception ex);
    void TrackChanged(string playbackId, MetadataWrapper metadata, int pos, PlaybackMetricsReason reason);

    void TrackPlayed(string playbackId, PlaybackMetricsReason reason, PlayerMetrics metrics, int endedt);
}