using CPlayerLib;
using Google.Protobuf;

namespace Eum.Cores.Spotify.Models
{
    internal readonly struct RawMercuryRequest
    {
        public readonly Header Header;
        public readonly List<byte[]> Payload;

        internal static RawMercuryRequest Get(string uri)
        {
            return new(uri, "GET");
        }

        internal static RawMercuryRequest Sub(string uri)
        {
            var r = new RawMercuryRequest(uri, "SUB");
            r.AddUserField("Accept-Language", "1202656e");
            return r;
        }

        internal static RawMercuryRequest Unsub(string uri)
        {
            return new(uri, "UNSUB");
        }

        internal RawMercuryRequest(string uri,
            string method,
            IEnumerable<MercuryRequest> multiRequests = null)
        {
            Payload = new List<byte[]>();
            Header = new Header
            {
                Method = method,
                Uri = uri
            };
            if (multiRequests == null) return;
            Header.ContentType = "vnd.spotify/mercury-mget-request";
            var multi = new MercuryMultiGetRequest();
            multi.Request.AddRange(multiRequests);
            Payload.Add(multi.ToByteArray());
        }

        public RawMercuryRequest ContentType(string contentType)
        {
            Header.ContentType = contentType;
            return this;
        }

        public void AddUserField(string key, string value)
        {
            AddUserField(new UserField
            {
                Key = key,
                Value = ByteString.CopyFromUtf8(value)
            });
        }

        public void AddUserField(UserField field)
        {
            Header.UserFields.Add(field);
        }
    }
}