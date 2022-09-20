using Connectstate;
using Google.Protobuf.Collections;

namespace Eum.Cores.Spotify.Contracts.Models;

public class CurrentlyPlayingState : IEquatable<CurrentlyPlayingState>
{
    public CurrentlyPlayingState(PlayerState cluster)
    {
        IsPaused = cluster.IsPaused;
        RepeatState =  (cluster?.Options?.RepeatingContext ?? false)
            ? RepeatStateType.RepeatContext
            : ((cluster?.Options?.RepeatingTrack ?? false)
                ? RepeatStateType.RepeatTrack
                : RepeatStateType.None);
        IsShuffle = cluster?.Options?.ShufflingContext ?? false;
        StartedPlayingAt = cluster.Timestamp; 
        Position_Old = cluster.Position;
        PositionAsOfTimestamp = cluster.PositionAsOfTimestamp;
        MetadataOriginal = cluster.Track.Metadata;
    }
    public ISpotifyAudioItem Item { get; init; }
    
    public bool IsPaused { get; init; }
    public bool IsShuffle { get; init; }
    public RepeatStateType RepeatState { get; init; }
    public double StartedPlayingAt { get; init; }
    public double PositionAsOfTimestamp { get; init; }

    public TimeSpan Position
    {
        get
        {
            if (IsPaused)
                return TimeSpan.FromMilliseconds(PositionAsOfTimestamp);
            
            var diff = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - StartedPlayingAt);
            var theoretical =
                (int)(PositionAsOfTimestamp + diff);
            return TimeSpan.FromMilliseconds(theoretical);
        }
    }
    
    public MapField<string, string> MetadataOriginal { get; init; }
    public long Position_Old { get; init; }

    public virtual bool Equals(CurrentlyPlayingState? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Item.Id == other.Item.Id;
    }
    public override int GetHashCode()
    {
        return Position_Old.GetHashCode();
    }
}