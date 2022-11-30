using System;
using System.IO;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Helpers;
using Eum.Logging;
using Google.Protobuf;
using Nito.AsyncEx;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify.Clients;

public class AudioKeyManager : IAudioKeyManager
{
    private volatile int seqHolder;
    private static readonly byte[] ZERO_SHORT = new byte[] {0, 0};
    private readonly ISpotifyConnectionProvider _spotifyConnectionProvider;
    public AudioKeyManager(ISpotifyConnectionProvider spotifyConnectionProvider)
    {
        _spotifyConnectionProvider = spotifyConnectionProvider;
    }

    public async Task<byte[]> GetAudioKey(ByteString trackGid, ByteString fileFileId, bool retry = true)
    {
        var connection = await _spotifyConnectionProvider.GetConnectionAsync();
      
        using var @out = new MemoryStream();
        fileFileId.WriteTo(@out);
        trackGid.WriteTo(@out);
        var b = Utils.toByteArray(seqHolder);
        @out.Write(b, 0, b.Length);
        @out.Write(ZERO_SHORT, 0, ZERO_SHORT.Length);

        var waitforkey = new AsyncManualResetEvent(false);
        var aeskey = default(AesKeyResponse);
        connection.RegisterKeyCallback(seqHolder, response =>
        {
            aeskey = response;
            waitforkey.Set();
        });
        await connection.SendPacketAsync(new MercuryPacket(MercuryPacketType.RequestKey, @out.ToArray()));

        await waitforkey.WaitAsync();
        if (aeskey.ErrorCode == -1)
        {
            return aeskey.Key;
        }

        S_Log.Instance.LogError($"Failed to get audio key for track {trackGid.ToByteArray().BytesToHex()} with error code {aeskey.ErrorCode}");
        if (retry) 
            return await GetAudioKey(trackGid, fileFileId, false);
        throw new AesKeyException(
            $"Failed fetching audio key! gid: " +
            $"{trackGid.ToByteArray().BytesToHex()}," +
            $" fileId: {fileFileId.ToByteArray().BytesToHex()}");
    }
}

public class AesKeyException : Exception
{
    public AesKeyException(string failedFetchingAudioKeyGid)
    {
        throw new NotImplementedException();
    }
}