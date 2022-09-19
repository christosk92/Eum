using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Web;
using Connectstate;
using Eum.Cores.Spotify.Connect.HttpHandlers;
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

    private async Task OnMessageReceived(ResponseMessage obj)
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
                var initial = await
                    _spClient.PutConnectState(connId,
                        _config.DeviceId,
                        ms);
                var test = Cluster.Parser.ParseFrom(initial);
                // LatestCluster = Cluster.Parser.ParseFrom(initial);
                ConnectionId = connId;
            }
            catch (Exception x)
            {
                await _wsClient.Stop(WebSocketCloseStatus.NormalClosure, "Error");
                _waitForConnectionId.Set();
                Dispose();
            }
        }
        else
        {

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

    public class CouldNotFindHeadersException : Exception
    {
    }
}