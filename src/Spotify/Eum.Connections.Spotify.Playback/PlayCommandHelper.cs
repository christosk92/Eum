using System.Text.Json;

namespace Eum.Connections.Spotify.Playback;

internal static class PlayCommandHelper
{
    public static string? GetContextUri(ref JsonElement dataObject)
    {
        if (dataObject.TryGetProperty("context", out var context))
        {
            if (context.TryGetProperty("uri", out var uri))
            {
                return uri.GetString();
            }
        }

        return null;
    }

    public static JsonElement GetPlayOrigin(ref JsonElement dataObject)
        => dataObject.GetProperty("play_origin");

    public static JsonElement GetPlayerOptions(ref JsonElement dataObject)
        => dataObject.GetProperty("options")
            .GetProperty("player_options_override");

    public static JsonElement GetContext(ref JsonElement dataObject)
    {
        return dataObject.GetProperty("context");
    }

    public static string? GetTrackUid(ref JsonElement dataObject)
    {
        if (!dataObject.TryGetProperty("options", out var parent))
        {
            return null;
        }

        if (!parent.TryGetProperty("skip_to", out parent))
        {
            return null;
        }

        if (!parent.TryGetProperty("track_uid", out var trackUid))
        {
            return null;
        }

        return trackUid.GetString();
    }

    public static string? GetSkipToTrackUri(ref JsonElement dataObject)
    {
        if (!dataObject.TryGetProperty("options", out var parent))
        {
            return null;
        }

        if (!parent.TryGetProperty("skip_to", out parent))
        {
            return null;
        }

        if (!parent.TryGetProperty("track_uri", out var trackUid))
        {
            return null;
        }

        return trackUid.GetString();
    }

    public static int? GetSkipToIndex(ref JsonElement dataObject)
    {
        if (!dataObject.TryGetProperty("options", out var parent))
        {
            return null;
        }

        if (!parent.TryGetProperty("skip_to", out parent))
        {
            return null;
        }

        if (!parent.TryGetProperty("track_index", out var trackUid))
        {
            return null;
        }

        return trackUid.GetInt32();
    }

    public static int? GetSeekTo(ref JsonElement dataObject)
    {
        if (!dataObject.TryGetProperty("options", out var parent))
        {
            return null;
        }

        if (!parent.TryGetProperty("seek_to", out var seekTo))
        {
            return null;
        }

        return seekTo.GetInt32();
    }

    public static bool IsPaused(ref JsonElement dataObject, bool b)
    {
        if (!dataObject.TryGetProperty("options", out var parent))
        {
            return b;
        }

        if (!parent.TryGetProperty("initially_paused", out var element))
        {
            return b;
        }


        return element.GetBoolean();
    }

    public static bool WillSkipToSomething(ref JsonElement dataObject)
    {
        if (!dataObject.TryGetProperty("options", out var parent))
        {
            return false;
        }

        if (!parent.TryGetProperty("skip_to", out parent))
        {
            return false;
        }

        return parent.TryGetProperty("track_uid", out _) || parent.TryGetProperty("track_uri", out _)
                                                         || parent.TryGetProperty("track_index", out _);
    }
}