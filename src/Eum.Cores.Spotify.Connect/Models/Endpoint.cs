namespace Eum.Cores.Spotify.Connect.Models;

public enum Endpoint
{
    Unknown,
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
    Error
}
public static class EndpointHelper
{
    public static Endpoint? StringToEndPoint(this string input)
    {
        return input switch
        {
            "play" => Endpoint.Play,
            "pause" => Endpoint.Pause,
            "resume" => Endpoint.Resume,
            "seek_to" => Endpoint.SeekTo,
            "skip_next" => Endpoint.SkipNext,
            "skip_prev" => Endpoint.SkipPrev,
            "set_shuffling_context" => Endpoint.SetShufflingContext,
            "set_repeating_context" => Endpoint.SetRepeatingContext,
            "set_repeating_track" => Endpoint.SetRepeatingTrack,
            "update_context" => Endpoint.UpdateContext,
            "set_queue" => Endpoint.SetQueue,
            "add_to_queue" => Endpoint.AddToQueue,
            "transfer" => Endpoint.Transfer,
            _ => Endpoint.Unknown
        };
    }
}