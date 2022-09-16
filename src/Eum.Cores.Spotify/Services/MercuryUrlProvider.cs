using Eum.Cores.Spotify.Contracts;

namespace Eum.Cores.Spotify.Services;

internal sealed class MercuryUrlProvider : IMercuryUrlProvider
{
    public string GetArtistUrl(string id, string locale)
        => $"hm://artist/v1/{id}/desktop?format=json&catalogue=premium&locale={locale}&cat=1";
}