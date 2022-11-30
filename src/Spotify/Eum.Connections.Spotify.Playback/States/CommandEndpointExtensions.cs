namespace Eum.Connections.Spotify.Playback.States;

public static class CommandEndpointExtensions
{
    public static CommandEndpoint StringToEndPoint(this string input)
    {
        return input switch
        {
            "play" => CommandEndpoint.Play,
            "pause" => CommandEndpoint.Pause,
            "resume" => CommandEndpoint.Resume,
            "seek_to" => CommandEndpoint.SeekTo,
            "skip_next" => CommandEndpoint.SkipNext,
            "skip_prev" => CommandEndpoint.SkipPrev,
            "set_shuffling_context" => CommandEndpoint.SetShufflingContext,
            "set_repeating_context" => CommandEndpoint.SetRepeatingContext,
            "set_repeating_track" => CommandEndpoint.SetRepeatingTrack,
            "update_context" => CommandEndpoint.UpdateContext,
            "set_queue" => CommandEndpoint.SetQueue,
            "add_to_queue" => CommandEndpoint.AddToQueue,
            "transfer" => CommandEndpoint.Transfer,
            _ => CommandEndpoint.Error
        };
    }
}