using System;
using System.Linq;
using Eum.Connections.Spotify.Playback.Contexts;
using Eum.Spotify.connectstate;

namespace Eum.Connections.Spotify.Playback.States;

public class RestrictionsManager
{
    public const string REASON_ENDLESS_CONTEXT = "endless_context";
    public const string REASON_NO_PREV_TRACK = "no_prev_track";
    public const string REASON_NO_NEXT_TRACK = "no_next_track";
    private Restrictions _restrictions;

    public RestrictionsManager(AbsSpotifyContext context)
    {
        _restrictions = new Restrictions();
        if (!context.IsFinite)
        {
            Disallow(AllowedAction.SHUFFLE, REASON_ENDLESS_CONTEXT);
            Disallow(AllowedAction.REPEAT_CONTEXT, REASON_ENDLESS_CONTEXT);
        }
    }

    public bool Can(AllowedAction action)
    {
        switch (action)
        {
            case AllowedAction.SHUFFLE:
                return !_restrictions.DisallowTogglingShuffleReasons.Any();
            case AllowedAction.REPEAT_CONTEXT:
                return !_restrictions.DisallowTogglingRepeatContextReasons.Any();
            case AllowedAction.REPEAT_TRACK:
                return !_restrictions.DisallowTogglingRepeatTrackReasons.Any();
            case AllowedAction.PAUSE:
                return !_restrictions.DisallowPausingReasons.Any();
            case AllowedAction.RESUME:
                return !_restrictions.DisallowResumingReasons.Any();
            case AllowedAction.SEEK:
                return !_restrictions.DisallowSeekingReasons.Any();
            case AllowedAction.SKIP_PREV:
                return !_restrictions.DisallowSkippingPrevReasons.Any();
            case AllowedAction.SKIP_NEXT:
                return !_restrictions.DisallowSkippingNextReasons.Any();
            default:
                throw new ArgumentOutOfRangeException("Unknown restriction for " + action);
        }
    }

    public void Allow(AllowedAction action)
    {
        switch (action)
        {
            case AllowedAction.SHUFFLE:
                _restrictions.DisallowTogglingShuffleReasons.Clear();
                break;
            case AllowedAction.REPEAT_CONTEXT:
                _restrictions.DisallowTogglingRepeatContextReasons.Clear();
                break;
            case AllowedAction.REPEAT_TRACK:
                _restrictions.DisallowTogglingRepeatTrackReasons.Clear();
                break;
            case AllowedAction.PAUSE:
                _restrictions.DisallowPausingReasons.Clear();
                break;
            case AllowedAction.RESUME:
                _restrictions.DisallowResumingReasons.Clear();
                break;
            case AllowedAction.SEEK:
                _restrictions.DisallowSeekingReasons.Clear();
                break;
            case AllowedAction.SKIP_PREV:
                _restrictions.DisallowSkippingPrevReasons.Clear();
                break;
            case AllowedAction.SKIP_NEXT:
                _restrictions.DisallowSkippingNextReasons.Clear();
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown restriction for " + action);
        }
    }

    public void Disallow(AllowedAction action, string reason)
    {
        Allow(action);

        switch (action)
        {
            case AllowedAction.SHUFFLE:
                _restrictions.DisallowTogglingShuffleReasons.Add(reason);
                break;
            case AllowedAction.REPEAT_CONTEXT:
                _restrictions.DisallowTogglingRepeatContextReasons.Add(reason);
                break;
            case AllowedAction.REPEAT_TRACK:
                _restrictions.DisallowTogglingRepeatTrackReasons.Add(reason);
                break;
            case AllowedAction.PAUSE:
                _restrictions.DisallowPausingReasons.Add(reason);
                break;
            case AllowedAction.RESUME:
                _restrictions.DisallowResumingReasons.Add(reason);
                break;
            case AllowedAction.SEEK:
                _restrictions.DisallowSeekingReasons.Add(reason);
                break;
            case AllowedAction.SKIP_PREV:
                _restrictions.DisallowSkippingPrevReasons.Add(reason);
                break;
            case AllowedAction.SKIP_NEXT:
                _restrictions.DisallowSkippingNextReasons.Add(reason);
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown restriction for " + action);
        }
    }

    public enum AllowedAction
    {
        SHUFFLE,
        REPEAT_CONTEXT,
        REPEAT_TRACK,
        PAUSE,
        RESUME,
        SEEK,
        SKIP_PREV,
        SKIP_NEXT
    }

    public Restrictions ToProto() => _restrictions;
}