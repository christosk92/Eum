using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Connections.Spotify.Models.Users;
using Eum.Enums;

namespace Eum.Connections.Spotify.Models.Library
{
    public class MercuryCollectionResponse
    {
        [JsonPropertyName("item")]
        public IEnumerable<MercuryCollectionItem> Items { get; init; }
    }

    public class MercuryCollectionItem
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; init; }
        [JsonPropertyName("added_at")]
        public long AddedAtTimeStamp { get; init; }

        public DateTimeOffset AddedAtDate => DateTimeOffset.FromUnixTimeSeconds(AddedAtTimeStamp);

        public SpotifyId Id
        {
            get
            {
                var bytes = Convert.FromBase64String(Identifier);
                var hex = BitConverter.ToString(bytes);
                var hexData = hex.Replace("-", "").ToLower();
                return SpotifyId.FromHex(hexData, Type);
            }
        }

        public bool Equals(SpotifyId other)
        {
            return Id.Equals(other);
        }

        protected bool Equals(MercuryCollectionItem other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MercuryCollectionItem)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        [JsonConverter(typeof(JsonStringEnumConverterWithSpan))]
        public EntityType Type { get; init; }
    }

    public class JsonStringEnumConverterWithSpan : JsonConverter<EntityType>
    {
        public override EntityType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ReadOnlySpan<char> r = reader.GetString()!.ToLower();
            return ToType(r);
        }

        public override void Write(Utf8JsonWriter writer, EntityType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        internal static EntityType ToType(ReadOnlySpan<char> r)
        {
            return r switch
            {
                "station" => EntityType.Station,
                "track" => EntityType.Track,
                "artist" => EntityType.Artist,
                "album" => EntityType.Album,
                "show" => EntityType.Show,
                "episode" => EntityType.Episode,
                "playlist" => EntityType.Playlist,
                "collection" => EntityType.Collection,
                "user" => EntityType.User,
                "local" => EntityType.Local,
                "device" => EntityType.Unknown,
                _ => 0,
            };
        }
    }
}
