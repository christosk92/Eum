using System.Collections.Generic;

namespace Eum.Connections.Spotify.Websocket;

public interface IMessageListener
{
    void OnMessage(string uri, Dictionary<string,string> headers, byte[] decodedPayload);
}