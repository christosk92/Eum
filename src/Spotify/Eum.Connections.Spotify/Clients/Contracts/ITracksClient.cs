namespace Eum.Connections.Spotify.Clients.Contracts;

public interface ITracksClient
{
    IOpenTracksClient OpenTracks { get; }
    IMercuryTracksClient MercuryTracks { get; }
}