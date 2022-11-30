namespace Eum.Connections.Spotify.Playback.States;

public enum CommandEndpoint
{
    Play,
    Pause,
    Resume,
    SeekTo,
    SkipNext,
    SkipPrev,
    SetShufflingContext,
    SetRepeatingContext,
    SetRepeatingTrack,
    UpdateContext,
    SetQueue,
    AddToQueue,
    Transfer,
    Error,
}