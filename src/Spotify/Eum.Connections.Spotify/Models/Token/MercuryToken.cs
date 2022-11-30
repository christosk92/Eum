using System;
using System.Text.Json.Serialization;

namespace Eum.Connections.Spotify.Models.Token
{
    public sealed class MercuryToken
    {
        private const int TokenExpireThreshold = 10;
        
        [JsonConstructor]
        public MercuryToken(string accessToken, int expiresIn) =>
            (AccessToken, ExpiresIn, CreatedAt) = (accessToken, expiresIn, DateTime.UtcNow);

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; internal set; }
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; }
        [JsonIgnore] public DateTime CreatedAt { get; }

        [JsonIgnore]
        public TimeSpan RemainingTime => CreatedAt.AddSeconds(ExpiresIn)
                                         - DateTime.UtcNow;

        public override string ToString() => RemainingTime.TotalMilliseconds > 0 ? AccessToken : "Expired";

        public bool Expired
            => !(RemainingTime.TotalMilliseconds > 0);
    }
    
}