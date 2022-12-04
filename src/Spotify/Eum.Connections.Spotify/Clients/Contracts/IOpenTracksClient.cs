using Eum.Connections.Spotify.Attributes;
using Refit;

namespace Eum.Connections.Spotify.Clients.Contracts;

[OpenUrlEndpoint]
public interface IOpenTracksClient
{
    [Get("/v1/track")]
    Task GetTrack();
}