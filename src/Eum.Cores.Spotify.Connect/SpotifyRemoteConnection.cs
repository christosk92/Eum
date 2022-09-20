using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Web;
using Connectstate;
using CPlayerLib.Player.Proto.Transfer;
using Eum.Cores.Spotify.Connect.HttpHandlers;
using Eum.Cores.Spotify.Connect.Models;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Contracts.Services;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Websocket.Client;

namespace Eum.Cores.Spotify.Connect;

public sealed class SpotifyRemoteConnection : ISpotifyRemoteConnection
{
    private readonly SpotifyConfig _config;
    private readonly AsyncManualResetEvent _waitForConnectionId;
    private readonly IReadOnlyList<IDisposable> _disposables;
    private readonly CancellationTokenSource _pingToken = new CancellationTokenSource();
    private readonly WebsocketClient _wsClient;
    private readonly ISpotifyBearerService _spotifyBearerService;
    private readonly ISpClient _spClient;
    private readonly SpotifyRequestStateHolder _requestStateHolder;

    private string? _connId;


    public SpotifyRemoteConnection(WebsocketClient websocketClient,
        ISpotifyBearerService spotifyBearerService,
        ISpClient spClient, IOptions<SpotifyConfig> config)
    {
        _requestStateHolder =
            new SpotifyRequestStateHolder(config);
        _wsClient = websocketClient;
        _spotifyBearerService = spotifyBearerService;
        _spClient = spClient;
        _config = config.Value;
        _waitForConnectionId = new AsyncManualResetEvent(false);
        var messageReceived = _wsClient.MessageReceived
            .Where(msg => msg.Text != null)
            .Where(msg => msg.Text.StartsWith("{"))
            .Subscribe((e) => OnMessageReceived(e));
        var disconnectionHappened = _wsClient.DisconnectionHappened.Subscribe((e) => OnDisconnected(e));
        _disposables = new[]
        {
            messageReceived,
            disconnectionHappened
        };

        _ = Task.Run(async () =>
        {
            while (!_pingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(20), _pingToken.Token);
                try
                {
                    _wsClient.Send("{\"type\":\"ping\"}");
                }
                catch (Exception x)
                {
                    Debugger.Break();
                }
            }

        }, _pingToken.Token);
    }

    public bool IsAlive
    {
        get
        {
            if (_disposed) return false;
            return _wsClient.IsRunning;
        }
    }

    public string? ConnectionId
    {
        get => _connId;
        set
        {
            if (_connId != value)
            {
                _connId = value;
                _waitForConnectionId.Set();
            }
        }
    }

    public async Task<bool> EnsureConnectedAsync(CancellationToken ct)
    {
        if (IsAlive) return true;
        
        await _wsClient.StartOrFail();
        await _waitForConnectionId.WaitAsync(ct);
        if (ConnectionId == null) return false;
        return true;
    }

    public event EventHandler<ClusterUpdate?>? ClusterUpdated;
    public event EventHandler<string>? Disconnected;

    private void OnMessageReceived(ResponseMessage obj)
    {
        using var jsonDocument = JsonDocument.Parse(obj.Text);
        if (!jsonDocument.RootElement.TryGetProperty("headers", out var headers_json))
        {
            throw new CouldNotFindHeadersException();
        }

        var headers = headers_json.Deserialize<Dictionary<string, string>>();
        Debug.Assert(headers != null, nameof(headers) + " != null");
        if (headers.ContainsKey("Spotify-Connection-Id"))
        {
            var connId =
                HttpUtility.UrlDecode(headers["Spotify-Connection-Id"],
                    Encoding.UTF8);

            Debug.WriteLine($"new con id: {connId}");

            //send device hello.

            var timestamp = (ulong) DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _requestStateHolder.SetHasBeenPlayingForMs(
                0);


            var putState = _requestStateHolder.PutStateRequest;
            putState.PutStateReason = PutStateReason.NewDevice;
            putState.ClientSideTimestamp = timestamp;
            putState.Device.PlayerState = _requestStateHolder.PlayerState;
            try
            {
                var asBytes = putState.ToByteArray();
                using var ms = new MemoryStream();
                using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    gzip.Write(asBytes, 0, asBytes.Length);
                }

                ms.Position = 0;
                var initial = _spClient.PutConnectState(connId,
                    _config.DeviceId,
                    ms).ConfigureAwait(false)
                    .GetAwaiter().GetResult();
                var test = Cluster.Parser.ParseFrom(initial);
                // LatestCluster = Cluster.Parser.ParseFrom(initial);
                ConnectionId = connId;
            }
            catch (Exception x)
            {
                _wsClient.Stop(WebSocketCloseStatus.NormalClosure, "Error").ConfigureAwait(false)
                    .GetAwaiter().GetResult();
                _waitForConnectionId.Set();
                Dispose();
            }
        }
        else
        {
            if (!jsonDocument.RootElement.TryGetProperty("type", out var type_message))
            {
                return;
            }

            switch (type_message.GetString())
            {
                case "request":
                {
                    Debug.Assert(obj != null, nameof(obj) + " != null");

                    var messageIdent =
                        jsonDocument.RootElement.TryGetProperty("message_ident", out var messageIdentJson)
                            ? messageIdentJson.GetString()
                            : null;
                    var key = jsonDocument.RootElement.TryGetProperty("key", out var keyJson)
                        ? keyJson.GetString()
                        : null;
                    var payload = jsonDocument.RootElement.GetProperty("payload");

                    using var @in = new MemoryStream();
                    using var outputStream =
                        new MemoryStream(Convert.FromBase64String(payload.GetProperty("compressed").ToString()));

                    if (headers["Transfer-Encoding"]?.Equals("gzip") ?? false)
                    {
                        using var decompressionStream = new GZipStream(outputStream, CompressionMode.Decompress);
                        decompressionStream.CopyTo(@in);
                        Debug.WriteLine($"Decompressed");
                        using var doc = JsonDocument.Parse(@in.ToArray());
                        payload = doc.RootElement.Clone();

                    }

                    var command = payload.Deserialize<RequestCommand>();
                    
                    break;
                }

                case "message":
                {
                    var uri = jsonDocument.RootElement.GetProperty("uri").GetString();
                    using var payloads = jsonDocument.RootElement.GetProperty("payloads").EnumerateArray();
                    
                    byte[] decodedPayload = null;
                    foreach (var payload in payloads)
                    {
                        if (headers.ContainsKey("Content-Type")
                            && (headers["Content-Type"].Equals("application/json") ||
                                headers["Content-Type"].Equals("text/plain")))
                        {
                            if (payloads.Count() > 1) throw new InvalidOperationException();
                            decodedPayload = Encoding.Default.GetBytes(payloads.ElementAtOrDefault(0).ToString());
                        }
                        else if (headers.Any())
                        {
                            var payloadsStr = new string[payloads.Count()];
                            for (var i = 0; i < payloads.Count(); i++) payloadsStr[i] = payloads.ElementAtOrDefault(i).ToString();
                            var x = string.Join("", payloadsStr);
                            using var @in = new MemoryStream();
                            using var outputStream = new MemoryStream(Convert.FromBase64String(x));
                            if (headers.ContainsKey("Transfer-Encoding")
                                && (headers["Transfer-Encoding"]?.Equals("gzip") ?? false))
                            {
                                using var decompressionStream = new GZipStream(outputStream, CompressionMode.Decompress);
                                decompressionStream.CopyTo(@in);
                                Debug.WriteLine("Decompressed");
                            }

                            decodedPayload = @in.ToArray();
                        }
                        else
                        {
                            Debug.WriteLine($"Unknown message; Possibly playlist update.. {uri}");
                        }
                    }

                    if (decodedPayload != null)
                    {
                        if (uri.StartsWith("hm://connect-state/v1/cluster"))
                        {  
                            var update = ClusterUpdate.Parser.ParseFrom(decodedPayload);
                            ClusterUpdated?.Invoke(this, update);
                        }
                    }
                    break;
                }
            }
        }
    }

    private bool _disposed;

  
    private void OnDisconnected(DisconnectionInfo obj)
    {
        if (obj.Type != DisconnectionType.Exit)
            Disconnected?.Invoke(this, obj.CloseStatusDescription);
    }

    private object _disposeLock = new object();
    public void Dispose()
    {
        lock (_disposeLock)
        {

            if (!_disposed)
            {
                _wsClient.Dispose();
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }

                try
                {
                    _pingToken?.Cancel();
                }
                catch (Exception)
                {

                }


                try
                {
                    _pingToken?.Dispose();
                }
                catch (Exception)
                {
                }

                _disposed = true;
            }
        }
    }

    public sealed class CouldNotFindHeadersException : Exception
    {
    }
}