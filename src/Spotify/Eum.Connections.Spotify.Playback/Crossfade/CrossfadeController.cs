using System;
using System.Text.Json;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.Metrics;
using Eum.Library.Logger.Helpers;
using Eum.Logging;

namespace Eum.Connections.Spotify.Playback.Crossfade;

public class CrossfadeController
{
    private FadeInterval? _fadeIn = null;
    private FadeInterval? _fadeOut = null;
    private FadeInterval? _activeInterval = null;
    private float _lastGain = 1;
    private int _fadeOverlap = 0;
    private readonly string _playbackId;
    private readonly int _trackDuration;
    private readonly Dictionary<PlaybackMetricsReason, FadeInterval> _fadeOutMap = new();
    private readonly Dictionary<PlaybackMetricsReason, FadeInterval> _fadeInMap = new();
    private readonly int _defaultFadeDuration;
    
    public CrossfadeController(string playbackId, int duration, IReadOnlyDictionary<string, string>? metadata, 
        SpotifyConfig config) {
        _playbackId = playbackId;
        _trackDuration = duration;
        _defaultFadeDuration = config.CrossfadeDuration;

        if (metadata.TryGetValue("audio.fade_out_uri", out var fadeOutUri))
        {
            FadeOutPlayable = new SpotifyId(fadeOutUri);
        }

        PopulateFadeIn(metadata);
        PopulateFadeOut(metadata);

        S_Log.Instance.LogInfo($"Loaded crossfade intervals id: {playbackId}, in: {JsonSerializer.Serialize(_fadeInMap)}, out: {JsonSerializer.Serialize(_fadeOutMap)}");
    }

    private void PopulateFadeIn(IReadOnlyDictionary<string, string> metadata)
    {
        int fadeInDuration = int.Parse(metadata.GetValueOrDefault("audio.fade_in_duration", "-1"));
        int fadeInStartTime = int.Parse(metadata.GetValueOrDefault("audio.fade_in_start_time", "-1"));
        using var jsonDocument = JsonDocument.Parse(
            metadata.GetValueOrDefault("audio.fade_in_curves", "[]"));
        var fadeInCurves = jsonDocument.RootElement.EnumerateArray();
        if (fadeInCurves.Count() > 1) throw new NotSupportedException(fadeInCurves.ToString());



        if (fadeInDuration != 0 && fadeInCurves.Any())
        {
            var fadeCurve = GetFadeCurve(ref fadeInCurves);
            _fadeInMap[PlaybackMetricsReason.TRACK_DONE]
                = new FadeInterval(fadeInStartTime, fadeInDuration,
                    LookupInterpolator.FromJson(ref fadeCurve));
        }
        else if (_defaultFadeDuration > 0)
        {
            _fadeInMap[PlaybackMetricsReason.TRACK_DONE] =
                new FadeInterval(0, _defaultFadeDuration, new LinearIncreasingInterpolator());
        }

        int fwdFadeInStartTime = int.Parse(metadata.GetValueOrDefault("audio.fwdbtn.fade_in_start_time", "-1"));
        int fwdFadeInDuration = int.Parse(metadata.GetValueOrDefault("audio.fwdbtn.fade_in_duration", "-1"));
        if (fwdFadeInDuration > 0)
            _fadeInMap[PlaybackMetricsReason.FORWARD_BTN] =
                new FadeInterval(fwdFadeInStartTime, fwdFadeInDuration, new LinearIncreasingInterpolator());

        int backFadeInStartTime = int.Parse(metadata.GetValueOrDefault("audio.backbtn.fade_in_start_time", "-1"));
        int backFadeInDuration = int.Parse(metadata.GetValueOrDefault("audio.backbtn.fade_in_duration", "-1"));
        if (backFadeInDuration > 0)
            _fadeInMap[PlaybackMetricsReason.BACK_BTN] =
                new FadeInterval(backFadeInStartTime, backFadeInDuration, new LinearIncreasingInterpolator());
    }

    private static JsonElement.ArrayEnumerator GetFadeCurve(ref JsonElement.ArrayEnumerator curves)
    {
        curves.MoveNext();
        var curve = curves.Current;
        if (curve.GetProperty("start_point").GetSingle() != 0 
            || Math.Abs(curve.GetProperty("end_point").GetSingle() - 1) > 0.001)
            throw new NotSupportedException();

        return curve.GetProperty("fade_curve")
            .EnumerateArray();
    }

    private void PopulateFadeOut(IReadOnlyDictionary<string, string> metadata)
    {
        int fadeOutDuration = int.Parse(metadata.GetValueOrDefault("audio.fade_out_duration", "-1"));
        int fadeOutStartTime = int.Parse(metadata.GetValueOrDefault("audio.fade_out_start_time", "-1"));
        using var jsonDocument = JsonDocument.Parse(
            metadata.GetValueOrDefault("audio.fade_out_curves", "[]"));
        var fadeInCurves = jsonDocument.RootElement.EnumerateArray();
        if (fadeInCurves.Count() > 1) throw new NotSupportedException(fadeInCurves.ToString());



        if (fadeOutDuration != 0 && fadeInCurves.Any())
        {
            var fadeCurve = GetFadeCurve(ref fadeInCurves);
            _fadeInMap[PlaybackMetricsReason.TRACK_DONE]
                = new FadeInterval(fadeOutStartTime, fadeOutDuration,
                    LookupInterpolator.FromJson(ref fadeCurve));

        }
        else if (_defaultFadeDuration > 0)
        {
            _fadeOutMap[PlaybackMetricsReason.TRACK_DONE] =
                new FadeInterval(_trackDuration - _defaultFadeDuration, _defaultFadeDuration,
                    new LinearDecreasingInterpolator());

        }

        int backFadeOutDuration = int.Parse(metadata.GetValueOrDefault("audio.backbtn.fade_out_duration", "-1"));
        if (backFadeOutDuration > 0)
            _fadeOutMap[PlaybackMetricsReason.BACK_BTN] =
                new PartialFadeInterval(backFadeOutDuration, new LinearDecreasingInterpolator());

        int fwdFadeOutDuration = int.Parse(metadata.GetValueOrDefault("audio.fwdbtn.fade_out_duration", "-1"));
        if (fwdFadeOutDuration > 0)
            _fadeOutMap[PlaybackMetricsReason.FORWARD_BTN] =
                new PartialFadeInterval(fwdFadeOutDuration, new LinearDecreasingInterpolator());
    }


    public bool IsSupported { get; } = false;
    public int FadeOverlap => throw new NotSupportedException();
    public SpotifyId? FadeOutPlayable { get; }

    public float GetGain(int pos)
    {
        if (_activeInterval == null && _fadeIn == null && _fadeOut == null)
            return _lastGain;

        if (_activeInterval != null && _activeInterval.End <= pos) {
            _lastGain = _activeInterval.Interpolator.Last;

            if (_activeInterval == _fadeIn) {
                _fadeIn = null;
                S_Log.Instance.LogInfo($"Cleared fade in. id: {_playbackId}");
            } else if (_activeInterval == _fadeOut)
            {
                _fadeOut = null;
                S_Log.Instance.LogInfo($"Cleared fade out. id: {_playbackId}");
            }

            _activeInterval = null;
        }

        if (_activeInterval == null) {
            if (_fadeIn != null && pos >= _fadeIn.Start && _fadeIn.End >= pos) {
                _activeInterval = _fadeIn;
                _fadeOverlap += _fadeIn.Duration;
            } else if (_fadeOut != null && pos >= _fadeOut.Start && _fadeOut.End >= pos) {
                _activeInterval = _fadeOut;
                _fadeOverlap += _fadeOut.Duration;
            }
        }

        if (_activeInterval == null) return _lastGain;

        return _lastGain = _activeInterval.Interpolate(pos);
    }

    public FadeInterval SelectFadeOut(PlaybackMetricsReason reason, bool customFade)
    {
        if ((!customFade && FadeOutPlayable != null) && reason == PlaybackMetricsReason.TRACK_DONE) {
            _fadeOut = null;
            _activeInterval = null;
            S_Log.Instance.LogInfo($"Cleared fade out because custom fade doesn't apply. id: {_playbackId}");
            return null;
        } else {
            _fadeOutMap.TryGetValue(reason, out _fadeOut);
            _activeInterval = null;
            S_Log.Instance.LogInfo($"Changed fade out. curr: {_fadeOut}, custom: {customFade}, why: {reason}, id: {_playbackId}");
            return _fadeOut;
        }
    }

    public int FadeOutStartTimeMin() {
        int fadeOutStartTime = -1;
        foreach (var fadeInterval in _fadeOutMap)
        {
            if(fadeInterval.Value is PartialFadeInterval) continue;

            if (fadeOutStartTime == -1 || fadeOutStartTime > fadeInterval.Value.Start)
                fadeOutStartTime = fadeInterval.Value.Start;
        }

        if (fadeOutStartTime == -1) return _trackDuration;
        return fadeOutStartTime;
    }

    public bool HasAnyFadeOut => _fadeOutMap.Any();

    public FadeInterval? SelectFadeIn(PlaybackMetricsReason lastReason, bool customFade)
    {
        if ((!customFade && FadeOutPlayable != null) && lastReason == PlaybackMetricsReason.TRACK_DONE)
        {
            _fadeIn = null;
            _activeInterval = null;
            S_Log.Instance.LogInfo("Cleared fade in because custom fade doesn't apply.");
            return null;
        }

        if (_fadeInMap.ContainsKey(lastReason))
        {
            _fadeIn = _fadeInMap[lastReason];
            _activeInterval = null;
            S_Log.Instance.LogInfo(
                $"Changed fade in. Curr: {_fadeIn}, custom: {customFade}, why: {lastReason}, id: {_playbackId}");
            return _fadeIn;
        }

        return null;
    }
}

internal class LinearDecreasingInterpolator : IGainInterpolator
{
    public float Interpolate(float pos)
    {
        return 1 - pos;
    }

    public float Last => 0;
}

internal class LinearIncreasingInterpolator : IGainInterpolator
{
    public float Interpolate(float pos)
    {
        return pos;
    }

    public float Last => 1;
}

internal class LookupInterpolator
{
    public static IGainInterpolator FromJson(ref JsonElement.ArrayEnumerator getFadeCurve)
    {
        throw new NotImplementedException();
    }
}