using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;

namespace Eum.Cores.Spotify.Services;

internal sealed class MercuryUrlProvider : IMercuryUrlProvider
{
    public string BearerTokenUrl(string[] scopes, string client_id)
    {
        return $"hm://keymaster/token/authenticated?scope={string.Join(",", scopes)}" +
               $"&client_id={client_id}&device_id=";
    }

    public string GetArtistUrl(string id, string locale)
        => $"hm://artist/v1/{id}/desktop?format=json&catalogue=premium&locale={locale}&cat=1";
}