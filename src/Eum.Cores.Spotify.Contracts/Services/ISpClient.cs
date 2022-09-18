using Connectstate;
using Refit;

namespace Eum.Cores.Spotify.Contracts.Services;

public interface ISpClient
{
    [Headers("Content-Type: application/x-protobuf", "Content-Encoding: gzip")]
    [Put("/connect-state/v1/devices/{deviceId}")]
    Task<Stream> PutConnectState(
        [Header("X-Spotify-Connection-Id")] string spotifyConnectionid,
        string deviceId,
        Stream body,
        CancellationToken ct = default);
}