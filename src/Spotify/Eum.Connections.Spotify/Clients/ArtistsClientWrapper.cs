using Eum.Connections.Spotify.Clients.Contracts;

namespace Eum.Connections.Spotify.Clients;

public class ArtistsClientWrapper : IArtistClient
{
    public ArtistsClientWrapper(IMercuryArtistClient mercury, IOpenArtistClient open)
    {
        Mercury = mercury;
        Open = open;
    }

    public IMercuryArtistClient Mercury { get; }
    public IOpenArtistClient Open { get; }
}