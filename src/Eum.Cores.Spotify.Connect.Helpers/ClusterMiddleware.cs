using Connectstate;
using Eum.Cores.Spotify.Connect.Helpers.Contracts;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.Models;

namespace Eum.Cores.Spotify.Connect.Helpers;

public sealed class ClusterMiddleware : IClusterMiddleware
{
    private Cluster? _latestCluster;
    private CurrentlyPlayingState? _currentlyPlaying;
    private bool _isPaused;
    private bool _isShuffle;
    private long _ts;
    private RepeatStateType _repeatState;

    private readonly ISpotifyRemote _remote;
    public ClusterMiddleware(ISpotifyRemote remote)
    {
        _remote = remote;
        _latestCluster = _remote.LatestReceivedCluster;
    }
    
    public event EventHandler<CurrentlyPlayingState>? CurrentyPlayingChanged;
    public event EventHandler<bool>? PauseChanged;
    public event EventHandler<bool>? ShuffleChanged;
    public event EventHandler<RepeatStateType>? RepeatStateChanged;
    public event EventHandler<long>? Seeked; 

    public void Disconnect()
    {
        _remote.ClusterUpdated -= RemoteOnClusterUpdated;
    }

    public void Connect()
    {
        _remote.ClusterUpdated += RemoteOnClusterUpdated;
    }

    private void RemoteOnClusterUpdated(object? sender, ClusterUpdate? e)
    {
        if (e?.Cluster == null) return;
        _latestCluster = e.Cluster;
        
        CurrentlyPlaying = new CurrentlyPlayingState(_latestCluster.PlayerState);
        IsPaused = _latestCluster.PlayerState?.IsPaused ?? true;
        IsShuffle = _latestCluster.PlayerState?.Options?.ShufflingContext ?? false;
        RepeatState = (_latestCluster.PlayerState?.Options?.RepeatingContext ?? false)
            ? RepeatStateType.RepeatContext
            : ((_latestCluster.PlayerState?.Options?.RepeatingTrack ?? false)
                ? RepeatStateType.RepeatTrack
                : RepeatStateType.None);

        Position = GetPosition(_latestCluster);
    }

    public CurrentlyPlayingState? CurrentlyPlaying
    {
        get => _currentlyPlaying;
        set
        {
            if (_currentlyPlaying?.Equals(value) ?? true) return;
            _currentlyPlaying = value;
            if (value != null)
                CurrentyPlayingChanged?.Invoke(this, value);
        }
    }
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (EqualityComparer<bool>.Default.Equals(_isPaused, value)) return;
            _isPaused = value;
            PauseChanged?.Invoke(this, value);
        }
    }  
    
    public bool IsShuffle
    {
        get => _isShuffle;
        set
        {
            if (EqualityComparer<bool>.Default.Equals(_isShuffle, value)) return;
            _isShuffle = value;
            ShuffleChanged?.Invoke(this, value);
        }
    }  
    public RepeatStateType RepeatState
    {
        get => _repeatState;
        set
        {
            if (EqualityComparer<RepeatStateType>.Default.Equals(_repeatState, value)) return;
            _repeatState = value;
            RepeatStateChanged?.Invoke(this, value);
        }
    }  
    
    public long Position
    {
        get => _ts;
        set
        {
            if (EqualityComparer<long>.Default.Equals(_ts, value)) return;
            _ts = value;
            Seeked?.Invoke(this, value);
        }
    }  
    internal static int GetPosition(Cluster cluster)
    {
        if (cluster.PlayerState.IsPaused)
            return (int)cluster.PlayerState.PositionAsOfTimestamp;
        var diff = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - cluster.PlayerState.Timestamp);
        return (int)(cluster.PlayerState.PositionAsOfTimestamp + diff);
    }

}