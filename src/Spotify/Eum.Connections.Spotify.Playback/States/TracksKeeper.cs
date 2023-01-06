using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.Contexts;
using Eum.Connections.Spotify.Playback.Helpers;
using Eum.Library.Logger.Helpers;
using Eum.Logging;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.metadata;
using Eum.Spotify.transfer;

namespace Eum.Connections.Spotify.Playback.States;

internal class TracksKeeper
{
    private const int MAX_PREV_TRACKS = 16;
    private const int MAX_NEXT_TRACKS = 48;
    private readonly LinkedList<ContextTrack> _queue = new();
    public readonly List<ContextTrack> Tracks = new();
    private readonly FisherYatesShuffle<ContextTrack> shuffle = new();

    public bool IsPlayingQueue { get; private set; }

    public bool CannotLoadMore { get; private set; }
    public bool IsPlayingFirst => _stateWrapper.State.Index?.Track == 0;

    public bool IsPlayingLast
    {
        get
        {
            if (CannotLoadMore && !_queue.Any()) return _stateWrapper.State.Index.Track == Tracks.Count;
            return false;
        }
    }

    public List<ContextTrack> Queue => _queue.ToList();

    private int shuffleKeepIndex = -1;

    private readonly StateWrapper _stateWrapper;
    private readonly PagesLoader _pages;

    public TracksKeeper(StateWrapper stateWrapper, PagesLoader pages)
    {
        _stateWrapper = stateWrapper;
        _pages = pages;
        CheckComplete();
    }

    private void UpdateTrackCount()
    {
        if (_stateWrapper.Context.IsFinite)
        {
            _stateWrapper.State.ContextMetadata["track_count"] = (Tracks.Count + _queue.Count).ToString();
        }
        else
        {
            _stateWrapper.State.ContextMetadata.Remove("track_count");
        }
    }

    private void CheckComplete()
    {
        if (CannotLoadMore) return;

        if (_stateWrapper.Context!.IsFinite)
        {
            int total_tracks = int.Parse(_stateWrapper.State.ContextMetadata.GetValueOrDefault("track_count", "-1"));
            if (total_tracks == -1) CannotLoadMore = false;
            else CannotLoadMore = total_tracks == Tracks.Count;
        }
        else
        {
            CannotLoadMore = false;
        }
    }

    private void SetCurrentTrackIndex(int index)
    {
        if (IsPlayingQueue)
            throw new ArgumentException();
        _stateWrapper.State.Index = new ContextIndex
        {
            Track = (uint) index
        };
        UpdateState();
    }

    public void UpdateState()
    {
        if (IsPlayingQueue)
        {
            var head = _queue.First;
            _queue.Remove(head);
            _stateWrapper.State.Track = ProtoUtils.ToProvidedTrack(head.Value, _stateWrapper.State.ContextUri);
        }
        else
        {
            _stateWrapper.State.Track = ProtoUtils.ToProvidedTrack(Tracks[(int) _stateWrapper.State.Index.Track],
                _stateWrapper.State.ContextUri);
        }

        UpdateLikeDislike();

        UpdateTrackDuration();
        UpdatePrevNextTracks();
    }

    private void UpdatePrevNextTracks()
    {
        var index = _stateWrapper.State.Index.Track;

        _stateWrapper.State.PrevTracks.Clear();
        for (uint i = Math.Max(0, index - MAX_PREV_TRACKS); i < index; i++)
            _stateWrapper.State.PrevTracks.Add(ProtoUtils.ToProvidedTrack(Tracks[(int) i],
                _stateWrapper.State.ContextUri));

        _stateWrapper.State.NextTracks.Clear();
        foreach (var contextTrack in _queue)
        {
            _stateWrapper.State.NextTracks.Add(ProtoUtils.ToProvidedTrack(contextTrack,
                _stateWrapper.State.ContextUri));
        }

        for (uint i = index + 1; i < Math.Min(Tracks.Count, index + 1 + MAX_NEXT_TRACKS); i++)
        {
            _stateWrapper.State.NextTracks.Add(ProtoUtils.ToProvidedTrack(Tracks[(int) i],
                _stateWrapper.State.ContextUri));
        }
    }

    public void UpdateTrackDuration()
    {
        var current = _stateWrapper.State.Track;
        if (current.Metadata.ContainsKey("duration"))
        {
            _stateWrapper.State.Duration = long.Parse(current.Metadata["duration"]);
        }
        else
        {
            _stateWrapper.State.Duration = 0L;
        }
    }

    private void UpdateLikeDislike()
    {
        var a = _stateWrapper.State.ContextMetadata.ContainsKey("like-feedback-selected");
        var a2 = "0";
        if (a)
            a2 = _stateWrapper.State.ContextMetadata["like-feedback-selected"];
        if (a2 == "1")
        {
            _stateWrapper.State.ContextMetadata["like-feedback-selected"] =
                _stateWrapper.State.Track.Metadata.ContainsKey("like-feedback-selected")
                    ? _stateWrapper.State.Track.Metadata["like-feedback-selected"]
                    : "0";
        }
        else
        {
            _stateWrapper.State.ContextMetadata.Remove("like-feedback-selected");
        }

        var b = _stateWrapper.State.ContextMetadata.ContainsKey("dislike-feedback-enabled");
        var b2 = "0";
        if (b)
            b2 = _stateWrapper.State.ContextMetadata["dislike-feedback-enabled"];
        if (b2 == "1")
        {
            _stateWrapper.State.ContextMetadata["dislike-feedback-selected"] =
                _stateWrapper.State.Track.Metadata.ContainsKey("dislike-feedback-selected")
                    ? _stateWrapper.State.Track.Metadata["dislike-feedback-selected"]
                    : "0";
        }
        else
        {
            _stateWrapper.State.ContextMetadata.Remove("dislike-feedback-selected");
        }
    }

    public async Task InitializeStart()
    {
        if (!CannotLoadMore)
        {
            if (!(await _pages.NextPage()))
                throw new ArgumentException();

            Tracks.Clear();
            if (_pages.CurrentPage != null)
            {
                Tracks.AddRange(await _pages.CurrentPage);
            }
        }

        CheckComplete();

        if (Tracks.Any(a => !ShouldPlay(a)))
            throw UnsupportedContextException.CannotPlayAnything;


        if (_stateWrapper.Context.IsFinite && _stateWrapper.State.Options.ShufflingContext)
        {
        }
        else
        {
            _stateWrapper.State.Options.ShufflingContext = false;
        }

        SetCurrentTrackIndex(0);
        if (!ShouldPlay(Tracks[(int) _stateWrapper.State.Index.Track]))
        {
            S_Log.Instance.LogInfo(
                $"Cannot play currently selected track. skipping : {_stateWrapper.CurrentPlayable.Value.Uri}");
        }
    }

    private bool ShouldPlay(ContextTrack contextTrack)
    {
        if (contextTrack.Metadata.TryGetValue("force_remove_reasons", out var force_remove))
        {
            if (!string.IsNullOrEmpty(force_remove))
                return false;
        }

        if (contextTrack.HasUri)
        {
            if (contextTrack.Uri.StartsWith("spotify:delimter") ||
                contextTrack.Uri.StartsWith("spotify:meta:delimter")) return false;
        }

        var filterExplicit =
            "1".Equals(_stateWrapper.SpotifyClient.AuthenticatedUser.ProductInfo.GetValueOrDefault("filter_explicit",
                "0"));
        if (!filterExplicit) return true;

        return !bool.Parse(contextTrack.Metadata.GetValueOrDefault("is_explicit", "false"));
    }

    public void UpdateTrackDuration(int trackDuration)
    {
        _stateWrapper.State.Duration = trackDuration;
        _stateWrapper.State.Track.Metadata["duration"] = trackDuration.ToString();
        UpdateMetadataFor(_stateWrapper.State.Index.Track, "duration", trackDuration.ToString());
    }

    private void UpdateMetadataFor(uint indexTrack, string duration, string toString)
    {
        var builder = Tracks[(int) indexTrack];
        builder.Metadata[duration] = toString;
        Tracks[(int) indexTrack] = builder;
    }

    // public async Task InitializeFrom(string trackUri)
    // {
    //     while (true)
    //     {
    //         if (await _pages.NextPage())
    //         {
    //             var newTracks = await _pages.CurrentPage;
    //
    //             var index = newTracks.FindIndex(a => a.Uri == trackUri);
    //             if (index == -1)
    //             {
    //                 S_Log.Instance.LogWarning("Did not find track. going to next page");
    //                 _tracks.AddRange(newTracks);
    //                 continue;
    //             }
    //
    //             index += _tracks.Count;
    //             _tracks.AddRange(newTracks);
    //
    //             SetCurrentTrackIndex(index);
    //             S_Log.Instance.LogInfo($"initialized current track index to {index}");
    //             break;
    //         }
    //         else
    //         {
    //             CannotLoadMore = true;
    //             UpdateTrackCount();
    //         }
    //     }
    //
    //     CheckComplete();
    //
    //
    //     if (_tracks.Any(a => !ShouldPlay(a)))
    //         throw UnsupportedContextException.CannotPlayAnything;
    //
    //     var getTrack = await _stateWrapper.SpotifyClient.Tracks.MercuryTracks
    //         .GetTrack((new SpotifyId(trackUri)).Id);
    //     EnrichCurrentTrack(getTrack);
    // }

    public async Task<bool> InitializeFrom(Func<List<ContextTrack>, int> func, ContextTrack pbCurrentTrack, Queue cmdQueue)
    {
        Tracks.Clear();
        _queue.Clear();
        var foundTrack = false;
        while (true)
        {
            //https://open.spotify.com/track/1kHEuJRasudLhjvnbfc4yS?si=01849fb8f8f144b6
            if (await _pages.NextPage())
            {
                var newTracks = await _pages.CurrentPage;
                var index = func(newTracks);
                if (index == -1)
                {
                    S_Log.Instance.LogWarning("Did not find track. going to next page");
                    if (newTracks != null)
                    {
                        Tracks.AddRange(newTracks);
                    }

                    continue;
                }
                else
                {
                    foundTrack = true;
                }


                index += Tracks.Count;
                if (newTracks != null)
                {
                    Tracks.AddRange(newTracks);
                }

                SetCurrentTrackIndex(index);
                S_Log.Instance.LogInfo($"initialized current track index: {index}");
                break;
            }
            else
            {
                CannotLoadMore = true;
                UpdateTrackCount();
                return false;
            }
        }

        if (cmdQueue != null)
        {
            foreach (var cmdQueueTrack in cmdQueue.Tracks)
            {
                _queue.AddLast(cmdQueueTrack);
            }

            IsPlayingQueue = cmdQueue.IsPlayingQueue;
            UpdateState();
        }

        CheckComplete();

        if (Tracks.Any(a => !ShouldPlay(a)))
            throw UnsupportedContextException.CannotPlayAnything;

        try
        {
            if (pbCurrentTrack != null)
                EnrichCurrentTrack(pbCurrentTrack);
        }
        catch (ArgumentException ex)
        {
            S_Log.Instance.LogWarning("Failed updating current track metadata." + ex);
        }

        if (!ShouldPlay(Tracks[(int) _stateWrapper.State.Index.Track]))
        {
            S_Log.Instance.LogWarning(
                $"Cannot play currently selected track, skipping: {_stateWrapper.CurrentPlayable.Value.Uri}");

            throw new NotImplementedException();
            var repeatTrack = _stateWrapper.State.Options.RepeatingTrack;
            if (repeatTrack) _stateWrapper.State.Options.RepeatingTrack = false;
            //NextPlayable(false);
            //state.getOptionsBuilder().setRepeatingTrack(repeatTrack);
        }

        return foundTrack;
    }

    private void EnrichCurrentTrack(@ContextTrack track)
    {
        if (IsPlayingQueue)
        {
            var builder = _stateWrapper.State.Track;
            ProtoUtils.EnrichTrack(builder, track);
        }
        else
        {
            int index = (int) _stateWrapper.State.Index.Track;
            var current = Tracks[index];
            ProtoUtils.EnrichTrack(current, track);
            Tracks[index] = current;
            _stateWrapper.State.Track = ProtoUtils.ToProvidedTrack(current, _stateWrapper.ContextUri);
        }
    }

    public async ValueTask<NextPlayableType> NextPlayable(bool autoplayEnabled)
    {
        if (_stateWrapper.State.Options.RepeatingTrack)
            return NextPlayableType.OK_REPEAT;

        if (_queue.Any())
        {
            IsPlayingQueue = true;
            UpdateState();

            if (!ShouldPlay(Tracks[(int) _stateWrapper.State.Index.Track]))
                return await NextPlayable(autoplayEnabled);
            return NextPlayableType.OK_PLAY;
        }

        IsPlayingQueue = false;

        var play = true;
        var next = await NextPlayableDoNotSet();
        if (next == null || next.Value.Index == -1)
        {
            if (!_stateWrapper.Context.IsFinite) return NextPlayableType.MISSING_TRACKS;

            if (_stateWrapper.IsRepeatingContext)
            {
                SetCurrentTrackIndex(0);
            }
            else
            {
                if (autoplayEnabled)
                {
                    return NextPlayableType.AUTOPLAY;
                }

                SetCurrentTrackIndex(0);
                play = false;
            }
        }
        else
        {
            SetCurrentTrackIndex(next.Value.Index);
        }

        if (play) return NextPlayableType.OK_PLAY;
        return NextPlayableType.OK_PAUSE;
    }

    public async Task<(SpotifyId Id, int Index)?> NextPlayableDoNotSet()
    {
        if (_stateWrapper.State.Options.RepeatingTrack)
            return (
                new SpotifyId(Tracks[(int) _stateWrapper.State.Index.Track].Gid,
                    SpotifyId.InferUriType(_stateWrapper.Context.Uri)),
                (int) _stateWrapper.State.Index.Track);
        if (_queue.Any())
            return (new SpotifyId(_queue.First.Value.Gid, SpotifyId.InferUriType(_stateWrapper.Context.Uri)),
                -1);

        var current = (int) _stateWrapper.State.Index.Track;
        if (current == Tracks.Count - 1)
        {
            if (_stateWrapper.State.Options.ShufflingContext || CannotLoadMore) return null;

            if (await _pages.NextPage())
            {
                Tracks.AddRange(await _pages.CurrentPage);
            }
            else
            {
                CannotLoadMore = true;
                UpdateTrackCount();
                return null;
            }
        }

        if (!_stateWrapper.Context.IsFinite && Tracks.Count - current <= 5)
        {
            if (await _pages.NextPage())
            {
                Tracks.AddRange(await _pages.CurrentPage);
                S_Log.Instance.LogInfo("Preloaded next page due to infinite context.");
            }
            else
            {
                S_Log.Instance.LogWarning("Couldn't load (pre)loaded next page of context.");
            }
        }

        int add = 1;
        while (true)
        {
            var track = Tracks[current + add];
            if (ShouldPlay((track))) break;
            add++;
        }

        return (new SpotifyId(Tracks[current + add].Gid, SpotifyId.InferUriType(_stateWrapper.Context.Uri)),
            add + current);
    }
}

public class FisherYatesShuffle<T>
{
    //TODO
    private readonly Random random;
    private volatile int currentSeed;
    private volatile int sizeForSeed = -1;

    private static int[] GetShuffleExchanges(int size, int seed)
    {
        int[] exchanges = new int[size - 1];
        var rand = new Random(seed);
        for (int i = size - 1; i > 0; i--)
        {
            int n = rand.Next(i + 1);
            exchanges[size - 1 - i] = n;
        }

        return exchanges;
    }

    public void Shuffle( List<T> list, bool saveSeed)
        => Shuffle(list, 0, list.Count, saveSeed);

    public void Shuffle( List<T> list, int from, int to, bool saveSeed)
    {
        var seed = random.Next();
        if (saveSeed) currentSeed = seed;

        var size = to - from;
        if (saveSeed) sizeForSeed = size;

        var exchanges = GetShuffleExchanges(size, seed);
        for (var i = size - 1; i > 0; i--)
        {
            var n = exchanges[size - 1 - i];
            list.Swap(from + n, from + i);
        }
    }

    public void Unshuffle( List<T> list) => Unshuffle(list, 0, list.Count);

    public void Unshuffle( List<T> list, int from, int to)
    {
        if (currentSeed == 0) throw new Exception("Current seed is zero!");
        if (sizeForSeed != to - from) throw new Exception("Size mismatch! Cannot unshuffle.");

        var size = to - from;
        var exchanges = GetShuffleExchanges(size, currentSeed);
        for (var i = 1; i < size; i++)
        {
            var n = exchanges[size - i - 1];
            list.Swap(from + n, from + i);
        }

        currentSeed = 0;
        sizeForSeed = -1;
    }

    public bool CanUnshuffle(int size) => currentSeed != 0 && sizeForSeed == size;
}