using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SpotifyTcp.Crypto;
using SpotifyTcp.Models;

namespace SpotifyTcp.Contracts;

public interface ISpotifyTcpClient : IEquatable<ISpotifyTcpClient>, IDisposable
{
    Guid ConnectionId { get; }
    /// <summary>
    /// Performs a client handshake with the spotify tcp server.
    /// </summary>
    /// <param name="ct">A cancellation token which may be used to cancel the ongoing task.</param>
    /// <returns>If a connection is already made, it will return null.</returns>
    /// <exception cref="IOException"></exception>
    Task<IMissingAuthenticationSpotifyConnection?> ConnectAsync(CancellationToken ct = default);

    Task<int> SendPacketAsync(MercuryPacket loginPacket, CancellationToken ct = default);

    Task<MercuryPacket> ReceivePacketAsync(Shannon cipher, CancellationToken ct = default);
    Task<MercuryPacket> WaitForPacketAsync(CancellationToken ct = default);
    void Disconnect();
    bool IsAlive { get; }
}