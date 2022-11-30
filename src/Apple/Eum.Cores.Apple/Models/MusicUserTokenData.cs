using System.Text.Json.Serialization;

namespace Eum.Cores.Apple.Models;

public sealed class MusicUserTokenData
{
    [JsonPropertyName("musicUserToken")]
    public string MusicUserToken { get; init; }
}