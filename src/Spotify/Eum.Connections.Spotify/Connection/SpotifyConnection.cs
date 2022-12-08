using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Users;
using Eum.Logging;
using Eum.Spotify;
using Google.Protobuf;
using Nito.AsyncEx;
using SpotifyTcp;
using SpotifyTcp.Contracts;
using SpotifyTcp.Exceptions;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify.Connection;

public class SpotifyConnection : ISpotifyConnection
{
    private readonly CancellationTokenSource _waitForPackagesCts = new CancellationTokenSource();
    private ISpotifyTcpClient? _tcpClient;
    private AuthenticatedSpotifyUser? _authenticatedSpotifyUser;

    private readonly AsyncLock _connectionLock = new AsyncLock();
    private readonly ISpotifyAuthentication _authenticationMethod;
    private readonly SpotifyConfig? _config;
    internal SpotifyConnection(ISpotifyAuthentication authenticationMethod, SpotifyConfig? config)
    {
        _authenticationMethod = authenticationMethod;
        _config = config;
    }

    public bool IsAliveAndWell
    {
        get
        {
            return _tcpClient is
            {
                IsAlive: true
            } && _authenticatedSpotifyUser is not null;
        }
    }

    public string? CountryCode
    {
        get;
        private set;
    }

    public async Task SendPacketAsync(MercuryPacket packet, CancellationToken ct = default)
    {
        await ConnectAsync(ct);
        if (IsAliveAndWell)
        {
            await _tcpClient!.SendPacketAsync(packet, ct);
            return;
        }
        throw new SpotifyConnectionException("Connection is not established");
    }

    private readonly ConcurrentDictionary<long, List<byte[]>>
    _partials = new ConcurrentDictionary<long, List<byte[]>>();

    public event EventHandler<MercuryPacket> KeyReceived;
    //
    private ConcurrentDictionary<long, (Stopwatch Sw, Action<MercuryResponse> OnCallback)> _mercuryCallbacks = new();
    public void RegisterMercuryCallback(int sequence, Action<MercuryResponse> action)
    {
        var sw_main = Stopwatch.StartNew();
        _mercuryCallbacks[sequence] = (sw_main, action);
        S_Log.Instance.LogInfo($"[{sequence}]: Starting to listen for mercury response");
    }

    public void RegisterKeyCallback(int sequence, Action<AesKeyResponse> action)
    {
        S_Log.Instance.LogInfo($"[{sequence}]: Starting to listen for mercury response");
        void OnPacketReceived(object? sender, MercuryPacket e)
        {
            if (e.Cmd is MercuryPacketType.AesKey or MercuryPacketType.AesKeyError)
            {
                using var payload = new MemoryStream(e.Payload);
                var seq = 0;
                var buffer = e.Payload;
                seq = e.Payload.getInt((int)payload.Position, true);
                if (seq == sequence)
                {
                    payload.Seek(4, SeekOrigin.Current);
                    KeyReceived -= OnPacketReceived;

                    switch (e.Cmd)
                    {
                        case MercuryPacketType.AesKey:
                            var key = new byte[16];
                            payload.Read(key, 0, key.Length);
                            action(new AesKeyResponse
                            {
                                Key = key
                            });
                            break;
                        case MercuryPacketType.AesKeyError:
                            var code = e.Payload.getShort((int)payload.Position, true);
                            payload.Seek(2, SeekOrigin.Current);
                            action(new AesKeyResponse
                            {
                                ErrorCode = code
                            });
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                //not our package.
            }
        }
        KeyReceived += OnPacketReceived;
    }


    private readonly AsyncManualResetEvent _waitForProductInfo = new AsyncManualResetEvent(false);
    private readonly AsyncManualResetEvent _waitForCountryCode = new AsyncManualResetEvent(false);
    public async ValueTask<AuthenticatedSpotifyUser> ConnectAsync(CancellationToken ct = default)
    {
        using var timeoutToken = new CancellationTokenSource();
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutToken.Token);
        timeoutToken.CancelAfter(TimeSpan.FromSeconds(5));
        using (await _connectionLock.LockAsync(timeoutToken.Token))
        {
            if (IsAliveAndWell)
                return _authenticatedSpotifyUser!;
            _tcpClient?.Dispose();
            var newTcpClient = new SpotifyTcpClient("ap-gae2.spotify.com", 4070);
            var connected = await newTcpClient.ConnectAsync(timeoutToken.Token);
            if (connected == null)
            {
                var apresponsemsg = new APResponseMessage();
                apresponsemsg.LoginFailed = new APLoginFailed
                {
                    ErrorCode = ErrorCode.ProtocolError,
                    ErrorDescription = "Failed to connect to Spotify"
                };
                throw new SpotifyConnectionException(apresponsemsg);
            }

            static Os GetOperatingSystem()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Os.Osx;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return Os.Linux;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Os.Windows;
                }

                return Os.Linux;
            }


            var cpuFamily = CpuFamily.CpuX8664;
            var os = GetOperatingSystem();

            var packet = new ClientResponseEncrypted()
            {
                LoginCredentials = _authenticationMethod.GetCredentials(),
                SystemInfo = new SystemInfo
                {
                    CpuFamily = cpuFamily,
                    Os = os,
                    SystemInformationString = "eum-1-1",
                    DeviceId = _config.DeviceId
                },
                VersionString = "eum 1"
            };

            var loginPacket = new MercuryPacket(MercuryPacketType.Login, packet.ToByteArray());
            await newTcpClient.SendPacketAsync(loginPacket, timeoutToken.Token);

            var (cmd, payload) = await newTcpClient.WaitForPacketAsync(linkedToken.Token);

            _tcpClient = newTcpClient;
            _ =  Task.Run(async () => await StartPackageListener());

            var apWelcome = cmd switch
            {
                MercuryPacketType.APWelcome => APWelcome.Parser.ParseFrom(payload),
                MercuryPacketType.AuthFailure => throw new SpotifyAuthenticationException(
                    APLoginFailed.Parser.ParseFrom(payload)),
                _ => throw new ArgumentOutOfRangeException(nameof(cmd), $"Did not expect {cmd} packet with data: {System.Text.Encoding.UTF8.GetString(payload)}")
            };

            await _waitForCountryCode.WaitAsync(linkedToken.Token);
            await _waitForProductInfo.WaitAsync(linkedToken.Token);
            _authenticatedSpotifyUser = new AuthenticatedSpotifyUser
            {
                Username = apWelcome.CanonicalUsername,
                ProductInfo = ProductInfo,
                CountryCode= CountryCode,
                ResuableCredentialsType = apWelcome.ReusableAuthCredentialsType,
                ReusableAuthCredentialsBase64 = apWelcome.ReusableAuthCredentials.ToBase64()
            };
            return _authenticatedSpotifyUser;
        }
    }

    private readonly AsyncLock _packageHandlerLock = new AsyncLock();
    private async Task StartPackageListener()
    {
        while (!_waitForPackagesCts.IsCancellationRequested)
        {
            try
            {
                var (cmd, payload) = await _tcpClient!.WaitForPacketAsync(_waitForPackagesCts.Token);

                _ = Task.Run(async () =>
                {
                    using (await _packageHandlerLock.LockAsync())
                    {
                        switch (cmd)
                        {
                            case MercuryPacketType.Ping:
                                S_Log.Instance.LogInfo("Receiving ping..");
                                try
                                {
                                    using var timeoutToken = new CancellationTokenSource();
                                    using var linked =
                                        CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token,
                                            _waitForPackagesCts.Token);
                                    timeoutToken.CancelAfter(TimeSpan.FromSeconds(5));
                                    await _tcpClient.SendPacketAsync(new MercuryPacket(MercuryPacketType.Pong,
                                        payload), linked.Token);
                                }
                                catch (IOException ex)
                                {
                                    S_Log.Instance.LogError("Failed sending Pong!", ex);
                                    Debugger.Break();
                                    //TODO: Reconnect
                                }

                                break;
                            case MercuryPacketType.CountryCode:
                                var countryCode = Encoding.UTF8.GetString(payload);
                                CountryCode = countryCode;
                                _waitForCountryCode.Set();
                                S_Log.Instance.LogInfo($"CountryCode: {countryCode}");
                                //ReceivedCountryCode = countryCode;
                                break;
                            case MercuryPacketType.PongAck:
                                break;
                            case MercuryPacketType.MercuryReq:
                            case MercuryPacketType.MercurySub:
                            case MercuryPacketType.MercuryUnsub:
                            case MercuryPacketType.MercuryEvent:
                                //Handle mercury packet..
                                Task.Run(() =>
                                {
                                    var sw = Stopwatch.StartNew();
                                    using var stream = new MemoryStream(payload);
                                    int seqLength = payload.getShort((int)stream.Position, true);
                                    stream.Seek(2, SeekOrigin.Current);
                                    long seq = 0;
                                    var buffer = payload;
                                    switch (seqLength)
                                    {
                                        case 2:
                                            seq = payload.getShort((int)stream.Position, true);
                                            stream.Seek(2, SeekOrigin.Current);
                                            break;
                                        case 4:
                                            seq = payload.getInt((int)stream.Position, true);
                                            stream.Seek(4, SeekOrigin.Current);
                                            break;
                                        case 8:
                                            seq = payload.getLong((int)stream.Position, true);
                                            stream.Seek(8, SeekOrigin.Current);
                                            break;
                                    }

                                    //proceed with decoding

                                    //sometimes the packages are too big to send at once, so spotify sends them in parallel, this meens we need to wait for all packages.
                                    var flags = payload[(int)stream.Position];
                                    stream.Seek(1, SeekOrigin.Current);
                                    var parts = payload.getShort((int)stream.Position, true);
                                    stream.Seek(2, SeekOrigin.Current);

                                    _partials.TryGetValue(seq, out var partial);
                                    partial ??= new List<byte[]>();
                                    if (!partial.Any() || flags == 0)
                                    {
                                        partial = new List<byte[]>();
                                        _partials.TryAdd(seq, partial);
                                    }


                                    S_Log.Instance.LogInfo("Handling packet, cmd: " +
                                                    $"{cmd}, seq: {seq}, flags: {flags}, parts: {parts}");

                                    for (var j = 0; j < parts; j++)
                                    {
                                        var size = payload.getShort((int)stream.Position, true);
                                        stream.Seek(2, SeekOrigin.Current);

                                        var buffer2 = new byte[size];

                                        var end = buffer2.Length;
                                        for (var z = 0; z < end; z++)
                                        {
                                            var a = payload[(int)stream.Position];
                                            stream.Seek(1, SeekOrigin.Current);
                                            buffer2[z] = a;
                                        }

                                        partial.Add(buffer2);
                                        _partials[seq] = partial;
                                    }

                                    if (flags != 1)
                                    {
                                        S_Log.Instance.LogInfo($"[{seq}]: Not all parts received yet, waiting for more. Took {sw.Elapsed}");
                                        return;
                                    };
                                    S_Log.Instance.LogInfo($"[{seq}]: All parts received, continuing. Took {sw.Elapsed}");


                                    _partials.TryRemove(seq, out partial);
                                    Header header;
                                    try
                                    {
                                        header = Header.Parser.ParseFrom(partial.First());
                                    }
                                    catch (Exception ex)
                                    {
                                        S_Log.Instance.LogError($"Couldn't parse header! bytes: {partial.First()}", ex);
                                        throw ex;
                                    }
                                    var resp = new MercuryResponse(header, partial, seq);
                                    switch (cmd)
                                    {
                                        case MercuryPacketType.MercuryReq:
                                            if (_mercuryCallbacks.TryRemove(seq, out var callBack))
                                            {
                                                callBack.Sw.Stop();
                                                S_Log.Instance.LogInfo(
                                                    $"[{seq}]: Finished handling mercury response. Took {callBack.Sw.Elapsed}");
                                                callBack.OnCallback(resp);
                                            }
                                            break;
                                        case MercuryPacketType.MercuryEvent:
                                            //Debug.WriteLine();
                                            break;
                                        default:
                                            Debugger.Break();
                                            break;
                                    }
                                });
                                // con.HandleMercury(newPacket);
                                break;
                            case MercuryPacketType.AesKeyError:
                            case MercuryPacketType.AesKey:
                                KeyReceived?.Invoke(this, new MercuryPacket(cmd, payload));
                                // HandleAesKey(newPacket);
                                break;
                            case MercuryPacketType.ProductInfo:
                                ProductInfo = ParseProductInfo(payload);
                                _waitForProductInfo.Set();
                                break;
                        }
                    }
                });
            }
            catch (Exception e)
            {
                S_Log.Instance.LogError(e);
                Debug.WriteLine(e);
                Dispose();
                break;
            }
        }
    }

    private IReadOnlyDictionary<string, string> ParseProductInfo(byte[] @in)
    {
        var attributes = new Dictionary<string, string>();
        var productInfoString = Encoding.Default.GetString(@in);
        S_Log.Instance.LogInfo($"productinfo: {productInfoString}");
        var xml = new XmlDocument();
        xml.LoadXml(productInfoString);

        var products = xml.SelectNodes("products");
        if (products != null && products.Count > 0)
        {
            var firstItemAsProducts = products[0];

            var product = firstItemAsProducts.ChildNodes[0];

            var properties = product.ChildNodes;
            for (var i = 0; i < properties.Count; i++)
            {
                var node = properties.Item(i);
                attributes[node.Name] = node.InnerText;
            }
        }

        return attributes;
    }
    public IReadOnlyDictionary<string, string> ProductInfo { get; private set; }

    private bool _requestedClose;
    public void CloseGracefully()
    {
        _requestedClose = true;
        _waitForPackagesCts.Cancel();
        try
        {
            _tcpClient?.Disconnect();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }
    public void Dispose()
    {
        _tcpClient?.Dispose();
        _waitForPackagesCts?.Dispose();
    }
}

public class AesKeyResponse
{
    public byte[] Key { get; init; }
    public int ErrorCode { get; init; } = -1;
}