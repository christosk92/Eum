using System.Text.Json;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;

namespace Eum.Connections.Spotify.Websocket;

public interface IRequestListener
{
    ValueTask<RequestResult> OnRequest(string mid, int pid, string? sender, JsonElement command);
}

public enum RequestResult
{
    UNKNOWN_SEND_COMMAND_RESULT, SUCCESS,
    DEVICE_NOT_FOUND, CONTEXT_PLAYER_ERROR,
    DEVICE_DISAPPEARED, UPSTREAM_ERROR,
    DEVICE_DOES_NOT_SUPPORT_COMMAND, RATE_LIMITED
}