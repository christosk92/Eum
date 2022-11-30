using System;
using Eum.Users;

namespace Eum;

public interface IMusicCore
{
    /// <summary>
    /// A boolean representing the state of the connection/auth. True if the client is connected AND authenticated, false otherwise.
    /// </summary>
    bool IsAuthenticated { get; }
    /// <summary>
    /// The current user of the music service. May be null if not authenticated.
    /// </summary>
    /// <exception cref="NotSupportedException">Music Service does not have an api for fetching information about the user.</exception>
    IUser? AuthenticatedUser { get; }
    
    CoreType Type { get; }
}

public enum CoreType
{
    Apple,
    Spotify
}