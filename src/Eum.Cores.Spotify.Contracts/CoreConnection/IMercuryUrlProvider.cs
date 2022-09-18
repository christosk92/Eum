
namespace Eum.Cores.Spotify.Contracts.CoreConnection;

public interface IMercuryUrlProvider
{
    string BearerTokenUrl(string[] scopes, string client_id);
    string GetArtistUrl(string id, string locale);
}