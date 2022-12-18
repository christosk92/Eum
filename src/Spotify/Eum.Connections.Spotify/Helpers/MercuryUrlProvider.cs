using Eum.Connections.Spotify.Models.Token;
using SpotifyTcp.Models;
using System.Diagnostics.Metrics;
using System;

namespace Eum.Connections.Spotify.Helpers;

internal static class MercuryUrlProvider
{
    public static string Bearer(string[] scopes, string client_id)
    {
        return $"hm://keymaster/token/authenticated?scope={string.Join(",", scopes)}" +
               $"&client_id={client_id}&device_id=";
    }

    public static string Artist(string id, string locale)
        => $"hm://artist/v1/{id}/desktop?format=json&catalogue=premium&locale={locale}&cat=1";

    public static string Album(string albumUri, string locale, string country)
        => $"hm://album/v1/album-app/album/{albumUri}/desktop?country={country}&catalogue=premium&locale={locale}";
}


public struct MercuryBearerRequest : IDefinedMercuryRequest<MercuryToken>
{
    public MercuryBearerRequest(string[] scopes, string clientId)
    {
        MercuryUrl = MercuryUrlProvider.Bearer(scopes, clientId);
    }

    public string MercuryUrl { get; }
}
