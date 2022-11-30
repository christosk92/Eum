namespace Eum.Connections.Spotify.Clients.Contracts;

public interface IArtistClient
{
    IMercuryArtistClient Mercury { get; }
    IOpenArtistClient Open { get; }
}