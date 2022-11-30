using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Exceptions;
using Eum.Spotify;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify.Clients.Contracts;

/// <summary>
///To communicate with specific backend services, spotify routes the requests through the AP using their own transport called "Hermes".
/// This is basically a URL scheme that lets the AP know where to send the request.
/// Payloads are encoded as Protobuf and follow the hm:// scheme.
/// </summary>
public interface IMercuryClient
{
    /// <summary>
    /// Fetches a mercury and returns the response as a raw byte array.
    /// </summary>
    /// <param name="mercuryUrl">The url to fetch, in the hm:// format.</param>
    /// <param name="type">The type of request. Defaults to GET.</param>
    /// <param name="ct">A cancellation token which may be used to cancel the ongoing task.</param>
    /// <returns>The payload in raw bytes.</returns>
    /// <exception cref="MercuryException"></exception>
    /// <exception cref="MissingAuthenticationException"></exception>
    Task<MercuryResponse> SendAndReceiveResponseAsync(string mercuryUrl,
        MercuryRequestType type = MercuryRequestType.Get,
        CancellationToken ct = default);

    /// <summary>
    /// Fetches a mercury and returns the response as a raw byte array.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="type">The type of request. Defaults to GET.</param>
    /// <param name="ct">A cancellation token which may be used to cancel the ongoing task.</param>
    /// <param name="mercuryUrl">The url to fetch, in the hm:// format.</param>
    /// <returns>The payload in raw bytes.</returns>
    /// <exception cref="MercuryException"></exception>
    /// <exception cref="MissingAuthenticationException"></exception>
    Task<MercuryResponse> SendAndReceiveResponseAsync(RawMercuryRequest request,
        MercuryRequestType type = MercuryRequestType.Get,
        CancellationToken ct = default);
    Task<T?> GetAsync<T>(IDefinedMercuryRequest<T> request, CancellationToken ct = default);
}
