using Eum.Connections.Spotify.Clients.Contracts;

namespace Eum.Connections.Spotify.Clients;

public class TracksClientWrapper : ITracksClient
{
    public TracksClientWrapper(IOpenTracksClient openTracks, IMercuryTracksClient mercuryTracks)
    {
        OpenTracks = openTracks;
        MercuryTracks = mercuryTracks;
    }

    public IOpenTracksClient OpenTracks { get; }
    public IMercuryTracksClient MercuryTracks { get; }
}