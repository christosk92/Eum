using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CPlayerLib;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Enums;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Helpers;
using Eum.Cores.Spotify.Models;
using Google.Protobuf;
using Nito.AsyncEx;

namespace Eum.Cores.Spotify.Connection;

public sealed class SpotifyConnection : ISpotifyConnection
{
    private readonly ITcpConnectionFactory _tcpConnectionFactory;
    private readonly LoginCredentials _loginCredentials;
    private readonly IApResolver _apResolver;

    private ITcpConnection? _currentConnection;
    private ISpotifyConnection _spotifyConnectionImplementation;

    private readonly Dictionary<long, List<byte[]>>
        _partials = new();

    private readonly Dictionary<long, (AsyncAutoResetEvent Waiter, MercuryResponse? Response)>
        _waiters = new();

    private CancellationTokenSource? _waitForPackagesToken;

    public SpotifyConnection(IApResolver apResolver, LoginCredentials loginCredentials,
        ITcpConnectionFactory tcpConnectionFactory)
    {
        _apResolver = apResolver;
        _loginCredentials = loginCredentials;
        _tcpConnectionFactory = tcpConnectionFactory;
        ConnectionId = Guid.NewGuid();
    }

    public Guid ConnectionId { get; }
    public string DeviceId { get; private set; }

    public bool IsAlive => APWelcome != null && _currentConnection is { IsAlive: true };
    public APWelcome? APWelcome { get; private set; }

    public async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (_currentConnection is
            {
                IsAlive: true
            })
            return;

        try
        {
            _waitForPackagesToken?.Cancel();
            _waitForPackagesToken?.Dispose();
        }
        catch (Exception)
        {
            // ignored
        }

        _currentConnection?.Dispose();

        var (host, port) = await _apResolver.GetClosestAccessPoint(ct);
        _currentConnection = _tcpConnectionFactory.GetTcpConnection(host, port, ct);

        DeviceId = Utils.RandomHexString(40).ToLower();
        var handshake = await _currentConnection.HandshakeAsync(ct);

        APWelcome = await _currentConnection.AuthenticateAsync(_loginCredentials, DeviceId, ct);
        _ = Task.Run(async () => await ListenForPacakges());
    }

    
    //TODO: Refactor package listener.
    #region REFACTOR_NEEDED
    private async Task ListenForPacakges()
    {
        _waitForPackagesToken = new CancellationTokenSource();
        while (!_waitForPackagesToken.IsCancellationRequested)
        {
            var newPacket = await _currentConnection.NextAsync(_waitForPackagesToken.Token);
            if (!Enum.TryParse(newPacket.Cmd.ToString(), out MercuryPacketType cmd))
            {
                Debug.WriteLine(
                    $"Skipping unknown command cmd: {newPacket.Cmd}," +
                    $" payload: {newPacket.Payload.BytesToHex()}");
                continue;
            }

            switch (cmd)
            {
                case MercuryPacketType.Ping:
                    Debug.WriteLine("Receiving ping..");
                    try
                    {
                        await _currentConnection.SendPacketAsync(new MercuryPacket(MercuryPacketType.Pong,
                            newPacket.Payload), _waitForPackagesToken.Token);
                    }
                    catch (IOException ex)
                    {
                        Debug.WriteLine("Failed sending Pong!", ex);
                        Debugger.Break();
                        //TODO: Reconnect
                    }

                    break;
                case MercuryPacketType.CountryCode:
                    var countryCode = Encoding.UTF8.GetString(newPacket.Payload);
                    //ReceivedCountryCode = countryCode;
                    break;
                case MercuryPacketType.PongAck:
                    break;
                case MercuryPacketType.MercuryReq:
                case MercuryPacketType.MercurySub:
                case MercuryPacketType.MercuryUnsub:
                case MercuryPacketType.MercuryEvent:
                    //Handle mercury packet..
                    // con.HandleMercury(newPacket);
                    HandleMercury(newPacket);
                    break;
                case MercuryPacketType.AesKeyError:
                case MercuryPacketType.AesKey:
                    // HandleAesKey(newPacket);
                    break;
                case MercuryPacketType.ProductInfo:
                    //ParseProductInfo(newPacket.Payload);
                    break;
            }
        }
    }

    public async Task<T> SendAndReceiveAsJson<T>(string mercuryUri,
        CancellationToken ct)
    {
        var response = await SendAndReceiveAsResponse(mercuryUri, MercuryRequestType.Get
            , ct);
        
        if (response is { StatusCode: >= 200 and < 300 })
        {
            return Deserialize<T>(response);
        }

        throw new MercuryException(response);
    }

    private async Task<MercuryResponse> SendAndReceiveAsResponse(string mercuryUri,
        MercuryRequestType type = MercuryRequestType.Get
        , CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);
        var sequence = Interlocked.Increment(ref _sequence);

        var req = type switch
        {
            MercuryRequestType.Get => RawMercuryRequest.Get(mercuryUri),
            MercuryRequestType.Sub => RawMercuryRequest.Sub(mercuryUri),
            MercuryRequestType.Unsub => RawMercuryRequest.Unsub(mercuryUri)
        };

        var requestPayload = req.Payload.ToArray();
        var requestHeader = req.Header;

        using var bytesOut = new MemoryStream();
        var s4B = BitConverter.GetBytes((short)4).Reverse().ToArray();
        bytesOut.Write(s4B, 0, s4B.Length); // Seq length

        var seqB = BitConverter.GetBytes(sequence).Reverse()
            .ToArray();
        bytesOut.Write(seqB, 0, seqB.Length); // Seq

        bytesOut.WriteByte(1); // Flags
        var reqpB = BitConverter.GetBytes((short)(1 + requestPayload.Length)).Reverse().ToArray();
        bytesOut.Write(reqpB, 0, reqpB.Length); // Parts count

        var headerBytes2 = requestHeader.ToByteArray();
        var hedBls = BitConverter.GetBytes((short)headerBytes2.Length).Reverse().ToArray();

        bytesOut.Write(hedBls, 0, hedBls.Length); // Header length
        bytesOut.Write(headerBytes2, 0, headerBytes2.Length); // Header


        foreach (var part in requestPayload)
        {
            // Parts
            var l = BitConverter.GetBytes((short)part.Length).Reverse().ToArray();
            bytesOut.Write(l, 0, l.Length);
            bytesOut.Write(part, 0, part.Length);
        }

        var cmd = type switch
        {
            MercuryRequestType.Sub => MercuryPacketType.MercurySub,
            MercuryRequestType.Unsub => MercuryPacketType.MercuryUnsub,
            _ => MercuryPacketType.MercuryReq
        };

        var wait = new AsyncAutoResetEvent(false);
        _waiters[sequence] = (wait, null);
        Debug.Assert(_currentConnection != null, nameof(_currentConnection) + " != null");
        await _currentConnection.SendPacketAsync(new MercuryPacket(cmd, bytesOut.ToArray()), ct);
        await wait.WaitAsync(ct);
        _waiters.Remove(sequence, out var a);
        return a.Response;
    }

    private void HandleMercury(MercuryPacket packet)
    {
        using var stream = new MemoryStream(packet.Payload);
        int seqLength = packet.Payload.getShort((int)stream.Position, true);
        stream.Seek(2, SeekOrigin.Current);
        long seq = 0;
        var buffer = packet.Payload;
        switch (seqLength)
        {
            case 2:
                seq = packet.Payload.getShort((int)stream.Position, true);
                stream.Seek(2, SeekOrigin.Current);
                break;
            case 4:
                seq = packet.Payload.getInt((int)stream.Position, true);
                stream.Seek(4, SeekOrigin.Current);
                break;
            case 8:
                seq = packet.Payload.getLong((int)stream.Position, true);
                stream.Seek(8, SeekOrigin.Current);
                break;
        }

        var flags = packet.Payload[(int)stream.Position];
        stream.Seek(1, SeekOrigin.Current);
        var parts = packet.Payload.getShort((int)stream.Position, true);
        stream.Seek(2, SeekOrigin.Current);

        _partials.TryGetValue(seq, out var partial);
        partial ??= new List<byte[]>();
        if (!partial.Any() || flags == 0)
        {
            partial = new List<byte[]>();
            _partials.TryAdd(seq, partial);
        }

        Debug.WriteLine("Handling packet, cmd: " +
                        $"{packet.Cmd}, seq: {seq}, flags: {flags}, parts: {parts}");

        for (var j = 0; j < parts; j++)
        {
            var size = packet.Payload.getShort((int)stream.Position, true);
            stream.Seek(2, SeekOrigin.Current);

            var buffer2 = new byte[size];

            var end = buffer2.Length;
            for (var z = 0; z < end; z++)
            {
                var a = packet.Payload[(int)stream.Position];
                stream.Seek(1, SeekOrigin.Current);
                buffer2[z] = a;
            }

            partial.Add(buffer2);
            _partials[seq] = partial;
        }

        if (flags != 1) return;

        _partials.Remove(seq, out partial);
        Header header;
        try
        {
            header = Header.Parser.ParseFrom(partial.First());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Couldn't parse header! bytes: {partial.First().BytesToHex()}");
            throw ex;
        }

        var resp = new MercuryResponse(header, partial, seq);
        switch (packet.Cmd)
        {
            case MercuryPacketType.MercuryReq:
                var a = _waiters[seq];
                a.Response = resp;
                _waiters[seq] = a;
                a.Waiter.Set();
                break;
            case MercuryPacketType.MercuryEvent:
                //Debug.WriteLine();
                break;
            default:
                Debugger.Break();
                break;
        }
    }

    #endregion
    public void Dispose()
    {
        _currentConnection?.Dispose();
        _waitForPackagesToken?.Dispose();
        APWelcome = null;
    }

    private volatile int _sequence;

    private static T Deserialize<T>(MercuryResponse resp) =>
        System.Text.Json.JsonSerializer.Deserialize<T>(
            new ReadOnlySpan<byte>(resp.Payload), opts);

    public static readonly JsonSerializerOptions opts = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
}

public class MercuryException : Exception
{
    public MercuryException(MercuryResponse response)
    {
        throw new NotImplementedException();
    }
}