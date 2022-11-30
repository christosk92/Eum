using System.Collections.Generic;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.States;
using Eum.Logging;
using Org.BouncyCastle.Asn1.X509;

namespace Eum.Connections.Spotify.Playback.Metrics;

public class PlaybackMetrics
{
    private readonly List<Interval> _intervals = new List<Interval>();
    private Interval? _lastInterval = null;

    public PlaybackMetrics(SpotifyId id, string playbackId, StateWrapper state, ITimeProvider timeProvider)
    {
        Id = id;
        PlaybackId = playbackId;
        ContextUri = state.ContextUri;
        FeatureVersion = state.State.PlayOrigin.FeatureVersion;
        ReferrerIdentifier = state.State.PlayOrigin.ReferrerIdentifier;
        Timestamp = timeProvider.CurrentTimeMillis();
    }
    public SpotifyId Id { get; }
    public string PlaybackId { get; }
    public string? ContextUri { get; }
    public string FeatureVersion { get; }
    public string ReferrerIdentifier { get; }
    public long Timestamp { get; }
    public PlaybackMetricsReason ReasonEnd { get; private set; }
    public string SourceEnd { get; private set; }
    public PlaybackMetricsReason ReasonStart { get; private set; }
    public string SourceStart { get; private set; }
    public PlayerMetrics? Player { get; private set; }
    
    public void EndedHow(PlaybackMetricsReason reason, string? origin)
    {
        ReasonEnd = reason;
        SourceEnd = origin == null || string.IsNullOrEmpty(origin) ? "unknown" : origin;
    }

    public void EndInterval(long end)
    {
        if (_lastInterval == null) return;
        if (_lastInterval.Begin == end)
        {
            _lastInterval = null;
            return;
        }

        _lastInterval.End = end;
        _intervals.Add(_lastInterval);
        _lastInterval = null;
    }

    public void Update(PlayerMetrics? metrics)
    {
        Player = metrics;
    }

    public void SendEvents(ISpotifyClient spotifyClient, DeviceStateHandler stateDevice)
    {
        if (Player == null || Player.ContentMetrics == null || stateDevice.LastCommandSentByDeviceId == null)
        {
            S_Log.Instance.LogWarning($"Did not send event because of missing metrics: {PlaybackId}");
        }
    }

    public void StartedHow(PlaybackMetricsReason reason, string? origin) {
        ReasonStart = reason;
        SourceStart = string.IsNullOrEmpty(origin) ? "unknown" : origin;
    }

    public void StartInterval(int pos)
    {
        _lastInterval = new Interval(pos);
    }
}

public record Interval(int Begin)
{
    public long End { get; set; } = -1;
}