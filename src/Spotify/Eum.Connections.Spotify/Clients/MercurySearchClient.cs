using Eum.Connections.Spotify.Clients.Contracts;
using SpotifyTcp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Eum.Connections.Spotify.Models.Search;
using System.Web;
using Org.BouncyCastle.Ocsp;
using System.Xml.Linq;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Artists;
using System.Text.Json;

namespace Eum.Connections.Spotify.Clients
{
    public sealed class MercurySearchClient : IMercurySearchClient
    {
        private readonly IMercuryClient _mercury;

        public MercurySearchClient(
            IMercuryClient mercury)
        {
            _mercury = mercury;
        }

        public async Task<FullSearchResponse> FullSearch(SearchRequest request,
            CancellationToken ct = default)
        {
            var buildUri = request.BuildUrl();

            var response = await
                _mercury.SendAndReceiveResponseAsync(buildUri, MercuryRequestType.Get, ct);

            if (response.StatusCode is >= 200 and < 300)
            {
                return JsonSerializer.Deserialize<FullSearchResponse>(response.Payload.Span, DefaultOptions.Default);
            }

            throw new MercuryException(response.Payload, response.StatusCode);
        }
    }

    public class SearchRequest
    {
        internal static readonly string MainSearch = "hm://searchview/km/v4/search/";
        internal static readonly string QuickSearch = "hm://searchview/km/v3/suggest/";

        private readonly string _query;

        public int Limit { get; set; }
        public string ImageSize { get; set; }
        public string Catalogue { get; set; }
        public string Country { get; set; }
        public string Locale { get; set; }
        public string Username { get; set; }
        public SearchRequest(
            string query,
            string imageSize,
            string catalogue,
            string country,
            string locale,
            string name,
            int limit = 4)
        {
            this._query = query.Trim();
            Limit = limit;
            ImageSize = imageSize;
            Catalogue = catalogue;
            Country = country;
            Locale = locale;
            Username = name;
        }
        internal string BuildUrl()
        {
            var url =
                Flurl.Url.Combine(MainSearch,
                    HttpUtility.UrlEncode(_query, Encoding.UTF8));
            url += "?entityVersion=2";
            url += "&limit=" + Limit;
            url += "&imageSize=" + HttpUtility.UrlEncode(ImageSize, Encoding.UTF8);
            url += "&catalogue=" + HttpUtility.UrlEncode(Catalogue, Encoding.UTF8);
            url += "&country=" + HttpUtility.UrlEncode(Country, Encoding.UTF8);
            url += "&locale=" + HttpUtility.UrlEncode(Locale, Encoding.UTF8);
            url += "&username=" + HttpUtility.UrlEncode(Username, Encoding.UTF8);
            return url;
        }
    }
}
