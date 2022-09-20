using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eum.Cores.Spotify.Connect.Models;

public readonly struct RequestCommand
{
    [JsonPropertyName("message_id")] public ulong Pid { get; init; }

    [JsonPropertyName("sent_by_device_id")]
    public string Sender { get; init; }

    public Endpoint Endpoint => (Command.TryGetProperty("endpoint", out var endpointStr)
        ? endpointStr
            .ToString().StringToEndPoint()
        : Endpoint.Error) ?? Endpoint.Unknown;

    [JsonPropertyName("command")] public JsonElement Command { get; init; }

    [JsonIgnore]
    public ReadOnlySpan<byte> Data
    {
        get
        {
            if (Command.TryGetProperty("data", out var data))
            {
                return Convert.FromBase64String(data.ToString());
            }
            return ReadOnlySpan<byte>.Empty;
        }
    }
}