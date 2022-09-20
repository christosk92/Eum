namespace Eum.Cores.Spotify.Connect.Helpers.Contracts;

public interface IClusterMiddleware
{
     /// <summary>
     /// Invoked whenever a new item has started playing on the remote device. Returns the spotify uri (spotify:track/episode:id) of the new item.
     /// </summary>
     event EventHandler<string>? CurrentyPlayingChanged;
     event EventHandler<bool>? PauseChanged;
     event EventHandler<bool>? ShuffleChanged;
     event EventHandler<RepeatStateType>? RepeatStateChanged;


     void Disconnect();
     void Connect();
     
     string? CurrentlyPlaying { get; }
     long Position { get; }
     bool IsPaused { get; }
     bool IsShuffle { get; }
     RepeatStateType RepeatState { get; }
}