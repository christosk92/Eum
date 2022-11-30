using System;

namespace Eum.Connections.Spotify.Playback.Exceptions;

public class FeederException : Exception
{
    public FeederException(string failedToResolveStorageInteractive = null) : base(failedToResolveStorageInteractive)
    {
        Message = failedToResolveStorageInteractive;
    }
    public string Message { get; }
}