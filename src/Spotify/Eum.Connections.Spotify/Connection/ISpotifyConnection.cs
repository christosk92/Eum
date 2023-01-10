using System;
using System.IO;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.Models.Users;
using SpotifyTcp.Exceptions;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify.Connection;

public interface ISpotifyConnection : IDisposable
{
    bool IsAliveAndWell { get; }
    /// <summary>
    /// Performs a connection (if not already done) to the spotify server.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="IOException">An issue occurred with the underlying TCP connection.</exception>
    /// <exception cref="TaskCanceledException">User requested cancellation</exception>
    /// <exception cref="OperationCanceledException">Timeout exception.</exception>
    /// <exception cref="SpotifyConnectionException">Unknown error with the spotify connection.</exception>
    /// <exception cref="UnknownDataException">Received wrong data.</exception>
    /// <exception cref="SpotifyAuthenticationException">Authentication error such as bad credentials etc.</exception>
    ValueTask<AuthenticatedSpotifyUser> ConnectAsync(CancellationToken ct= default);
    void CloseGracefully();
    
    string? CountryCode
    {
        get;
    }
    
    Task SendPacketAsync(MercuryPacket packet,
        CancellationToken ct = default);

    void RegisterMercuryCallback(int sequence, Action<MercuryResponse> action);
    void RegisterKeyCallback(int sequence, Action<AesKeyResponse> action);

    event EventHandler<IReadOnlyList<CollectionUpdate>>? CollectionUpdate;
}

public sealed class CollectionUpdate
{
    //{"items":[{"type":"track","unheard":false,"addedAt":0,"removed":true,"identifier":"0QPYn15U8IQHKcH2LDfrek"},{"type":"track","unheard":false,"addedAt":0,"removed":true,"identifier":"2FRnf9qhLbvw8fu4IBXx78"}]}
    public SpotifyId Id { get; init; }
    public bool Removed { get; init; }
    public DateTimeOffset? AddedAt { get; init; }
}