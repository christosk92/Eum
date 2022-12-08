using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Logging;
using Google.Protobuf;
using Nito.AsyncEx;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify.Clients;

public class MercuryClient : IMercuryClient
{
    private int _sequenceNumber = 0;
    private readonly ISpotifyConnectionProvider _connectionHolder;

    public MercuryClient(ISpotifyConnectionProvider connectionHolder)
    {
        _connectionHolder = connectionHolder;
    }

    public async Task<MercuryResponse> SendAndReceiveResponseAsync(string mercuryUrl,
        MercuryRequestType type = MercuryRequestType.Get,
        CancellationToken ct = default)
    {
        var req = type switch
        {
            MercuryRequestType.Get => RawMercuryRequest.Get(mercuryUrl),
            MercuryRequestType.Subscribe => RawMercuryRequest.Sub(mercuryUrl),
            MercuryRequestType.Unsuscribe => RawMercuryRequest.Unsub(mercuryUrl)
        };

        return await SendAndReceiveResponseAsync(req, type, ct);
    }

    public async Task<MercuryResponse> SendAndReceiveResponseAsync(RawMercuryRequest req, MercuryRequestType type = MercuryRequestType.Get, CancellationToken ct = default)
    {
        var spotifyConnection = await _connectionHolder.GetConnectionAsync(ct: ct);

        var sequence = Interlocked.Increment(ref _sequenceNumber);

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
            MercuryRequestType.Subscribe => MercuryPacketType.MercurySub,
            MercuryRequestType.Unsuscribe => MercuryPacketType.MercuryUnsub,
            _ => MercuryPacketType.MercuryReq
        };
        using var timeoutToken = new CancellationTokenSource();
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, ct);
        timeoutToken.CancelAfter(TimeSpan.FromSeconds(5));

        MercuryResponse response = default;
        //send a packet and then wait for a response with the same sequence number
        var waitForPayload = new AsyncManualResetEvent(false);
        spotifyConnection.RegisterMercuryCallback(sequence, (payload) =>
        {
            response = payload;
            waitForPayload.Set();
        });

        var sw = Stopwatch.StartNew();
        S_Log.Instance.LogInfo($"Sending mercury request to {req.Header.Uri} with sequence {sequence} and cmd {cmd}");
        _ = Task.Run(async () =>
                await spotifyConnection.SendPacketAsync(new MercuryPacket(cmd, bytesOut.ToArray()), linkedToken.Token),
            linkedToken.Token);
        await waitForPayload.WaitAsync(linkedToken.Token);

        sw.Stop();
        S_Log.Instance.LogInfo($"Mercury request to {req.Header.Uri} with sequence {sequence} and cmd {cmd} " +
                               $"took {sw.Elapsed}ms");
        if (response.StatusCode is >= 200 and < 300)
        {
            S_Log.Instance.LogInfo($"Mercury request {sequence} successful");
            return response;
        }

        S_Log.Instance.LogWarning($"Mercury request {sequence} with status code " + response.StatusCode);
        throw new MercuryException(response.Payload, response.StatusCode);
    }

    public async Task<T?> GetAsync<T>(IDefinedMercuryRequest<T> request, CancellationToken ct = default)
    {
        var response = await SendAndReceiveResponseAsync(request.MercuryUrl, MercuryRequestType.Get, ct);
        var deserialized = JsonSerializer.Deserialize<T>(response.Payload.Span, DefaultOptions.Default);
        return deserialized;
    }
}