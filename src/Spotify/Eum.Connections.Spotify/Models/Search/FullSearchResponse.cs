
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.JsonConverters;

namespace Eum.Connections.Spotify.Models.Search
{
    public class FullSearchResponse
    {
        public Dictionary<string, SearchHitsObject<ISpotifyItem>> Results { get; set; }
        public string RequestId { get; set; }

        public List<string> CategoriesOrder { get; set; }
    }
    public class SearchHitsObject<T>
    {
        [JsonConverter(typeof(MercuryTypeConverterToISpotifyItem))]
        public T[] Hits { get; set; }
        public long Total { get; set; }
        public int Count => Hits.Length;
    }
}