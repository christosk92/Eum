using System.Globalization;
using Refit;

namespace Eum.Connections.Spotify.Models.Views
{
    public readonly struct HomeRequest
    {
        private readonly string _country;

        public HomeRequest(DateTimeOffset? time = null,
            string locale = "en",
            string country = "US")
        {
            Timestamp = (time ?? DateTimeOffset.Now)
                .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'");
            Country = country;
            Locale = locale;
        }
        public HomeRequest(
            CultureInfo culture)
        {
            var r = new RegionInfo(culture.LCID);
            var locale = r.Name.Replace("-", "_");
            var country = r.TwoLetterISORegionName;
            Timestamp =DateTimeOffset.Now
                .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'");
            Country = country;
            Locale = locale;
        }

        [AliasAs("timestamp")]
        public string Timestamp { get; }

        [AliasAs("content_limit")]
        public int ContentLimit
        {
            get;
            init;
        } = 5;

        [AliasAs("locale")]
        public string Locale { get; }

        public string Country { get; }

        [AliasAs("types")]
        [Query(CollectionFormat.Csv)]
        public string[] Types { get; init; } = new[]
        {
            "track",
            "album",
            "playlist",
            "playlist_v2",
            "artist",
            "collection_artist",
            "collection_album"
        };

        [AliasAs("limit")]
        public int Limit { get; init; } = 5;
        [AliasAs("offset")]
        public int Offset { get; init; } = 0;
    }
}
