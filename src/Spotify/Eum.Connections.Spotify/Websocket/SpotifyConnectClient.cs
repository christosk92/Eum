using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Spotify.connectstate;
using Nito.AsyncEx;
using Polly;
using Websocket.Client.Exceptions;

namespace Eum.Connections.Spotify.Websocket;

public class SpotifyConnectClient : ISpotifyConnectClient
{
    private readonly AsyncManualResetEvent _waitForReqListeners = new AsyncManualResetEvent();
    private readonly AsyncManualResetEvent _waitForMsgListeners = new AsyncManualResetEvent();
    private readonly ConcurrentDictionary<IMessageListener, List<string>> msgListeners = new();
    private readonly ConcurrentDictionary<string, IRequestListener> reqListeners = new();


    private static readonly IAsyncPolicy<bool>
        RetryConnectionToWebsocketPolicy = Policy<bool>
            .Handle<WebsocketException>()
            .OrResult(a => !a)
            .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(4));

    private IDisposable? _clientListener;
    private readonly IBearerClient _bearerClient;
    private readonly ISpotifyWebsocket _spotifyWebsocket;

    public SpotifyConnectClient(ISpotifyWebsocket spotifyWebsocket, IBearerClient bearerClient)
    {
        _spotifyWebsocket = spotifyWebsocket;
        _bearerClient = bearerClient;
        _clientListener = _spotifyWebsocket.DealerEventHappened.Subscribe((a) =>
        {
            Task.Run(async () => await OnMessageReceived(a));
        });
    }

    //var token = await _bearerService.GetBearerTokenAsync(ct);
    public bool WaitingForConnectionId => _spotifyWebsocket.WaitingForConnectionId;
    public string? ConnectionId => _spotifyWebsocket.ConnectionId;
    public Uri? ConnectionUri => _spotifyWebsocket.ConnectionUri;
    public bool Connected => _spotifyWebsocket.Connected;

    public event EventHandler<string?> ConnectionIdReceived;

    public async Task<bool> Authenticate(CancellationToken ct = default)
    {
        var token = await _bearerClient.GetBearerTokenAsync(ct);
        return await _spotifyWebsocket.Authenticate(token, ct);
    }

    private AsyncLock _connectionLock = new AsyncLock();

    private async Task OnMessageReceived((DealerEventType EventType, object? Parameter) obj)
    {
        switch (obj.EventType)
        {
            case DealerEventType.DISCONNECTED:
                if (obj.Parameter is SocketCloseDescription closeDescription)
                {
                    if (closeDescription.Message != "internal-close")
                    {
                        using (await _connectionLock.LockAsync())
                        {
                            if (!Connected)
                            {
                                //reconnect..
                                await RetryConnectionToWebsocketPolicy.ExecuteAsync(() => Authenticate());
                            }
                        }
                    }
                }

                break;
            case DealerEventType.AUTHENTICATED:
                break;
            case DealerEventType.CONNECTION_ID:
                ConnectionIdReceived?.Invoke(this, ConnectionId);
                break;
            case DealerEventType.AUTHENTICATION_FAILED:
                break;
            case DealerEventType.ERROR:
                break;
            case DealerEventType.MESSAGE:
            {
                Task.Run(async () =>
                {
                    await WaitForListeners();
                    if (obj.Parameter is JsonElement jsonElement)
                    {
                        var uri = jsonElement.GetProperty("uri")
                            .GetString();

                        using var payloads = jsonElement.GetProperty("payloads").EnumerateArray();
                        if (!jsonElement.TryGetProperty("headers", out var headers_json))
                        {
                            throw new Exception();
                        }

                        var headers = headers_json.Deserialize<Dictionary<string, string>>();
                        Debug.Assert(headers != null, nameof(headers) + " != null");

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
                                for (var i = 0; i < payloads.Count(); i++)
                                    payloadsStr[i] = payloads.ElementAtOrDefault(i).ToString();
                                var x = string.Join("", payloadsStr);
                                using var @in = new MemoryStream();
                                using var outputStream = new MemoryStream(Convert.FromBase64String(x));
                                if (headers.ContainsKey("Transfer-Encoding")
                                    && (headers["Transfer-Encoding"]?.Equals("gzip") ?? false))
                                {
                                    using var decompressionStream =
                                        new GZipStream(outputStream, CompressionMode.Decompress);
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
                            bool interesting = false;
                            lock (msgListeners)
                            {
                                foreach (var msgListener in msgListeners)
                                {
                                    bool dispatched = false;
                                    foreach (var key in msgListener.Value)
                                    {
                                        if (uri.StartsWith(key) && !dispatched)
                                        {
                                            interesting = true;
                                            Task.Run(() =>
                                            {
                                                msgListener.Key.OnMessage(uri, headers, decodedPayload);
                                            });
                                            dispatched = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }
                break;
            case DealerEventType.REQUEST:
                Task.Run(async () =>
                {
                    await WaitForListeners();
                    if (obj.Parameter is JsonElement jsonElement)
                    {
                        var mid = jsonElement.GetProperty("message_ident").GetString();
                        var key = jsonElement.GetProperty("key").GetString();
                        if (!jsonElement.TryGetProperty("headers", out var headers_json))
                        {
                            throw new Exception();
                        }

                        var headers = headers_json.Deserialize<Dictionary<string, string>>();
                        Debug.Assert(headers != null, nameof(headers) + " != null");

                        var payload = jsonElement.GetProperty("payload");

                        using var @in = new MemoryStream();
                        using var outputStream =
                            new MemoryStream(Convert.FromBase64String(payload.GetProperty("compressed").ToString()));
                        if (headers["Transfer-Encoding"]?.Equals("gzip") ?? false)
                        {
                            using var decompressionStream = new GZipStream(outputStream, CompressionMode.Decompress);
                            await decompressionStream.CopyToAsync(@in);
                            Debug.WriteLine($"Decompressed");
                            @in.Position = 0;
                            using var doc = await JsonDocument.ParseAsync(@in);
                            payload = doc.RootElement.Clone();
                        }

                        var pid = payload.GetProperty("message_id").GetInt32();
                        var sender = payload.GetProperty("sent_by_device_id").GetString();

                        var command = payload.GetProperty("command");
                        Debug.WriteLine("Received request. mid: {0}, key: {1}, pid: {2}, sender: {3}", mid, key, pid,
                            sender);
                        var interesting = false;

                        foreach (var midprefix in reqListeners)
                        {
                            if (mid.StartsWith(midprefix.Key))
                            {
                                var listener = reqListeners[midprefix.Key];
                                interesting = true;
                                var result = await listener.OnRequest(mid, pid, sender, command);
                                await SendReply(key, result);
                                Debug.WriteLine("Handled request. key: {0}, result: {1}", key, result);
                            }
                        }

                        if (!interesting) Debug.WriteLine("Couldn't dispatch request: " + mid);
                    }
                });
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    async Task SendReply(string key, RequestResult result)
    {
        var success = result == RequestResult.SUCCESS;
        var reply = string.Format("{{\"type\":\"reply\", \"key\": \"{0}\", \"payload\": {{\"success\": {1}}}}}", key.ToLower(),
            success.ToString().ToLowerInvariant());
        await _spotifyWebsocket.SendMessageAsync(reply);
    }

    public void AddMessageListener(IMessageListener deviceStateHandler, params string[] uris)
    {
        msgListeners.AddOrUpdate(deviceStateHandler, uris.ToList(),
            (key, value) => uris
                .Concat(value).Distinct().ToList());
        _waitForMsgListeners.Set();
    }

    public void AddRequestListener(IRequestListener deviceStateHandler, params string[] uris)
    {
        foreach (var uri in uris)
        {
            reqListeners.AddOrUpdate(uri, deviceStateHandler,
                (key, value) => deviceStateHandler);
            _waitForReqListeners.Set();
        }
    }


    private async Task WaitForListeners()
    {
        var waitForMsgs = Task.Run(async () => await _waitForMsgListeners.WaitAsync());
        var waitforReqs = Task.Run(async () => await _waitForReqListeners.WaitAsync());

        await Task.WhenAll(waitforReqs, waitForMsgs);
    }

    public void Disconnect()
    {
        _spotifyWebsocket.Disconnect("internal-close");
    }
}