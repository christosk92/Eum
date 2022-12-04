using System;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Logging;
using Nito.AsyncEx;
using Websocket.Client;
using Websocket.Client.Exceptions;
using Uri = System.Uri;

namespace Eum.Connections.Spotify.Websocket
{

    public class SpotifyWebSocket : ISpotifyWebsocket
    {
        private bool _wasConnected;
        private IDisposable[] _disposables = new IDisposable[2];

        
        const int HEARTBEAT_INTERVAL = 25000;
        private static Regex CONNECTION_ID_EXP = new Regex(@"hm:\/\/pusher\/(?:[^*]+)?\/connections\/([^*]+)");
        private readonly Subject<(DealerEventType EventType, object? Parameter)> _messageReceivedSubject = new();
        private readonly IBearerClient _bearerService;
        private WebsocketClient? _socket;

        public SpotifyWebSocket(IBearerClient bearerService)
        {
            _bearerService = bearerService;
        }

        private CancellationTokenSource _heartbeatToken = new CancellationTokenSource();
        private readonly AsyncManualResetEvent _waitForPong = new AsyncManualResetEvent();

        /// <summary>
        /// The amount of time to wait before triggering a heartbeat timeout
        /// </summary>
        const int HEARTBEAT_TIMEOUT = 10000;

        /// <summary>
        /// A boolean flag that indicates whether we're waiting for a Connection Id. <br/>
        /// When set to true, the next "connection id" message will result in a similarevent.
        /// </summary>
        public bool WaitingForConnectionId { get; private set; } = true;

        /// <summary>
        /// The string connection id for the Dealer connection.
        /// </summary>
        public string? ConnectionId { get; private set; }

        /// <summary>
        /// The string connection id uri for the Dealer connection.
        /// </summary>
        public Uri? ConnectionUri { get; private set; }

        public async Task<bool> Authenticate(string token, CancellationToken ct = default)
        {
            var url = new Uri($"wss://gae2-dealer.spotify.com?access_token={token}");
            if (_socket is not null)
            {
                Disconnect("internal-close");
            }

            try
            {
                _heartbeatToken?.Dispose();
            }
            catch{}


            _heartbeatToken = new CancellationTokenSource();
            WaitingForConnectionId = true;
            try
            {
                S_Log.Instance.LogInfo($"Initializing new websocket.");
                var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
                {
                    Options =
                    {
                        KeepAliveInterval = TimeSpan.FromDays(7),
                    },
                });
                var socket = new WebsocketClient(url, factory);
                socket.ReconnectTimeout = null;
                socket.IsReconnectionEnabled = false;
                socket.ErrorReconnectTimeout = null;

                _disposables[0] = socket.DisconnectionHappened.Subscribe(HandleDisconnect);
                _disposables[1] =
                    socket.MessageReceived.Subscribe((msg) => { HandleMessage(msg); });

                _socket = socket;
                await socket.StartOrFail();
                S_Log.Instance.LogInfo($"Started new websocket.");
                _messageReceivedSubject.OnNext((DealerEventType.AUTHENTICATED, null));
                _wasConnected = true;
                return true;
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                throw;
            }

            return false;
        }

        public void Disconnect(string reason)
        {
            if (_socket == null) return;

            S_Log.Instance.LogInfo($"Closing previous connection with connectionid: {ConnectionId ?? "null"}");
            try
            {
                _heartbeatToken.Cancel();
            } catch{}

            WaitingForConnectionId = true;

            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }

            this._socket.Stop(WebSocketCloseStatus.NormalClosure, reason);
            _socket?.Dispose();
            _messageReceivedSubject.OnNext((DealerEventType.DISCONNECTED, new SocketCloseDescription
            {
                Message = reason,
                Info = null
            }));
            _wasConnected = false;
        }

        public async Task SendMessageAsync(string reply)
        {
            await _socket.SendInstant(reply);
        }

        public bool Connected =>
            _socket is
            {
                IsStarted: true
            } && ConnectionId != null;

        private void HandleDisconnect(DisconnectionInfo obj)
        {
            S_Log.Instance.LogInfo("Websocket disconnected.");
            if (!_wasConnected)
            {
                //log DisconnectionInfo and its properties:

                if (obj.Exception is WebsocketException ws)
                {
                    if (ws.Message == "The server returned status code '401' when status code '101' was expected.")
                    {
                        S_Log.Instance.LogInfo(
                            $"Authentication error: Type: {obj.Type} \r\n Exception: {obj.Exception} \r\n CloseStatus: {obj.CloseStatus} \r\n CloseStatusDescription: {obj.CloseStatusDescription}");
                        _messageReceivedSubject.OnNext((DealerEventType.AUTHENTICATION_FAILED,
                            new SocketCloseDescription()
                            {
                                Message = "authentication-error",
                                Info = obj
                            }));
                    }
                    else
                    {     
                        S_Log.Instance.LogInfo(
                            $"Socket connection error: Type: {obj.Type} \r\n Exception: {obj.Exception} \r\n CloseStatus: {obj.CloseStatus} \r\n CloseStatusDescription: {obj.CloseStatusDescription}");
                        _messageReceivedSubject.OnNext((DealerEventType.AUTHENTICATION_FAILED,
                            new SocketCloseDescription()
                            {
                                Message = "dealer-connection-error",
                                Info = obj
                            }));
                        
                    }
                }

                return;
            }
            else
            {
                S_Log.Instance.LogInfo(
                    $"Random error: Type: {obj.Type} \r\n Exception: {obj.Exception} \r\n CloseStatus: {obj.CloseStatus} \r\n CloseStatusDescription: {obj.CloseStatusDescription}");
                _messageReceivedSubject.OnNext((DealerEventType.DISCONNECTED, new SocketCloseDescription
                {
                    Message = "dealer-connection-error",
                    Info = obj
                }));
            }
        }

        private void HandleMessage(ResponseMessage obj)
        {
            try
            {
                using var jsonDocument = JsonDocument.Parse(obj.Text);

                var type = jsonDocument.RootElement.GetProperty("type")
                    .GetString();
                switch (type)
                {
                    case "message":
                        if (WaitingForConnectionId && PrepareConnectionId(jsonDocument.RootElement.Clone()))
                        {
                            WaitingForConnectionId = false;
                            Task.Run(async () => await StartHeartbeat());
                        }
                        else
                        {
                            _messageReceivedSubject.OnNext(
                                ((DealerEventType.MESSAGE, jsonDocument.RootElement.Clone())));
                        }

                        break;
                    case "pong":
                        S_Log.Instance.LogInfo("Received pong...");
                        _waitForPong.Set();
                        break;
                    case "request":
                        _messageReceivedSubject.OnNext(
                            ((DealerEventType.REQUEST, jsonDocument.RootElement.Clone())));
                        break;
                }
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
            }
        }

        private bool PrepareConnectionId(JsonElement message)
        {
            if (!message.TryGetProperty("uri", out var uri))
            {
                return false;
            }

            var matches = CONNECTION_ID_EXP.Match(uri.GetString());
            if (!matches.Success)
            {
                return false;
            }

            string id;
            if (message.TryGetProperty("headers", out var headers) &&
                headers.TryGetProperty("Spotify-Connection-Id", out var connId))
            {
                // Header in ID is not URI encoded.
                id = connId.GetString();
            }
            else
            {
                id = System.Uri.EscapeDataString(matches.Groups[1].Value);
            }

            ConnectionId = id;
            ConnectionUri = new Uri(uri.GetString());
            _messageReceivedSubject.OnNext((DealerEventType.CONNECTION_ID,id));
            return true;
        }

        private async Task StartHeartbeat(bool initial = false)
        {
            var _heartbeatTimeoutToken = new CancellationTokenSource();
            while (!_heartbeatToken.IsCancellationRequested
                   && !_heartbeatTimeoutToken.IsCancellationRequested)
            {
                await HeartBeat();

                try
                {
                    _heartbeatTimeoutToken.CancelAfter(HEARTBEAT_TIMEOUT);
                    await _waitForPong.WaitAsync(_heartbeatTimeoutToken.Token);
                    _waitForPong.Reset();
                    await Task.Delay(HEARTBEAT_INTERVAL, _heartbeatToken.Token);
                    
                    _heartbeatTimeoutToken.Dispose();
                    _heartbeatTimeoutToken = new CancellationTokenSource();
                }
                catch (TaskCanceledException x)
                {
                    S_Log.Instance.LogError(x);
                    Disconnect("internal-timeout");
                    // _messageReceivedSubject.OnNext((DealerEventType.DISCONNECTED, new SocketCloseDescription
                    // {
                    //     Message = "HEARTBEAT_TIMEOUT"
                    // }));   
                }
                catch (Exception x)
                {
                    S_Log.Instance.LogError(x);
                }
            }
        }

        private async Task HeartBeat()
        {
            try
            {
                await Ping();
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                OnHeartbeatError();
            }
        }

        private async Task Ping()
        {
            if (!Connected)
            {
                return;
            }

            S_Log.Instance.LogInfo("Sending ping...");
            await this._socket.SendInstant("{\"type\":\"ping\"}");
        }

        private void OnHeartbeatError()
        {
            if (_socket is null)
            {
                return;
            }

            _socket.Stop(WebSocketCloseStatus.ProtocolError, "internal-timeout");
        }


        /// <summary>Stream with received message (raw format)</summary>
        public IObservable<(DealerEventType EventType, object? Parameter)> DealerEventHappened =>
            _messageReceivedSubject.AsObservable();
    }

    internal class SocketCloseDescription
    {
        public string Message { get; init; }
        public DisconnectionInfo? Info { get; init; }
    }
}