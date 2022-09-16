
namespace Eum.Cores.Spotify.Contracts;

public interface IMercuryUrlProvider
{
    string GetArtistUrl(string id, string locale);
}