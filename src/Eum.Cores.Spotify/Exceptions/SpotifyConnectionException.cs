using CPlayerLib;

namespace Eum.Cores.Spotify.Exceptions;

public sealed class SpotifyConnectionException : Exception
{
    public SpotifyConnectionException(APResponseMessage apResponseMessage)
    {
        ApResponseMessage = apResponseMessage;
    }

    public readonly APResponseMessage ApResponseMessage;
}