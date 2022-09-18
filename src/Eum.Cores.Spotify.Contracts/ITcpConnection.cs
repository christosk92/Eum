using CPlayerLib;
using Eum.Cores.Spotify.Contracts.Models;

namespace Eum.Cores.Spotify.Contracts;

public interface ITcpConnection : IDisposable
{
    
    /// <summary>
    /// Initiate a client hello (handshake) with the server.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>The written data to the server, as a byte array. Use this to verify the signature.</returns>
    Task<bool> HandshakeAsync(CancellationToken ct = default);

    Task<APWelcome> AuthenticateAsync(
        LoginCredentials credentials,
        string deviceId,
        CancellationToken ct = default);

    Task SendPacketAsync(MercuryPacket packet, CancellationToken ct = default);
    Task<MercuryPacket> NextAsync(CancellationToken ct = default);
    
    bool IsAlive { get; }
}