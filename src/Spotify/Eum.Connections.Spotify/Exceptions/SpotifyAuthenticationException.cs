using System;
using Eum.Spotify;

namespace Eum.Connections.Spotify.Exceptions;

public sealed class SpotifyAuthenticationException : Exception
{
    public SpotifyAuthenticationException(APLoginFailed parseFrom)
    {
        LoginFailed = parseFrom;
    }
    public APLoginFailed LoginFailed { get; }
}