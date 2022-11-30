using System;
using Eum.Spotify;

namespace SpotifyTcp.Exceptions;

public sealed class SpotifyConnectionException : Exception
{
    public SpotifyConnectionException(APResponseMessage apResponseMessage)
    {
        ApResponseMessage = apResponseMessage;
    }

    public readonly APResponseMessage ApResponseMessage;

    public SpotifyConnectionException(string connectionIsNotEstablished)
    {
        ApResponseMessage = new APResponseMessage
        {
            LoginFailed = new APLoginFailed
            {
                ErrorDescription = connectionIsNotEstablished, ErrorCode = ErrorCode.ProtocolError
            }
        };
    }
}