using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.metadata;

namespace Eum.Connections.Spotify.Clients.Contracts;

public interface IMercuryTracksClient
{
    Task<Track> GetTrack(string id, CancellationToken ct = default);
}