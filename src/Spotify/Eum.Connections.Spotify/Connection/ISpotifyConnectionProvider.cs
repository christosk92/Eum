using System;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.Models.Users;

namespace Eum.Connections.Spotify.Connection;

public interface ISpotifyConnectionProvider : IDisposable
{
    bool IsConnected { get; }
    AuthenticatedSpotifyUser? GetCurrentUser();

    /// <summary>
    /// Connect and authenticate to Spotify. 
    /// </summary>
    /// <exception cref="MissingAuthenticationException">Thrown whenever no initial call has been made using the parameter.</exception>
    /// <param name="authenticator">Supply it with an authenticator if not done already. Only works once.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask<ISpotifyConnection?> GetConnectionAsync(ISpotifyAuthentication? authenticator = null,
        CancellationToken ct = default);

    event EventHandler<(ISpotifyConnection? Old, ISpotifyConnection? New)> NewConnection;
}