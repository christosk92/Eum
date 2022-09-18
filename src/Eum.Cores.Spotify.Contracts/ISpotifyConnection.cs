
using CPlayerLib;

namespace Eum.Cores.Spotify.Contracts;

public interface ISpotifyConnection : IDisposable
{
    Guid ConnectionId { get; }
    string? DeviceId { get; }
    APWelcome? APWelcome { get; }
    bool IsAlive { get; }
    Task EnsureConnectedAsync(CancellationToken ct = default);
}