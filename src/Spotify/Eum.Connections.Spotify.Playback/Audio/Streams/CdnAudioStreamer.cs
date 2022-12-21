using System.Diagnostics;
using Eum.Connections.Spotify.Audio;
using Eum.Connections.Spotify.Cache;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Playback.Audio.Storage;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Logging;
using Flurl.Http;
using Flurl.Util;
using Google.Protobuf;
using Nito.AsyncEx;
using SpotifyTcp.Helpers;

namespace Eum.Connections.Spotify.Playback.Audio.Streams;

public class CdnAudioStreamer : IDecodedAudioStream, IGeneralWritableStream
{
    public readonly ByteString FileId;
    private readonly CdnUrl _cdnUrl;
    private readonly SuperAudioFormat _format;
    private readonly IAudioDecrypt _audioDecrypt;
    private readonly ICacheHandler? _cacheHandler;
    private readonly ISpotifyClient _spotifyClient;

    private static readonly int CHUNK_SIZE = AesAudioDecrypt.CHUNK_SIZE;

    private readonly InternalCdnStream _internalStream;

    public CdnAudioStreamer(
        ISpotifyClient spotifyClient,
        ByteString fileId,
        CdnUrl cdnUrl,
        SuperAudioFormat format,
        IAudioDecrypt audioDecrypt,
        string name,
        ICacheManager? cache = null,
        IHaltListener? haltListener = null,
        bool isEpisode = false)
    {
        _spotifyClient = spotifyClient;
        FileId = fileId;
        _cdnUrl = cdnUrl;
        _format = format;
        _audioDecrypt = audioDecrypt;
        _cacheHandler = cache?.GetHandler(episodeId: isEpisode ? fileId : null, trackId: isEpisode ? null : fileId);

        bool fromCache;
        byte[] firstChunk;
        byte[] sizeHeader;

        int chunks = -1;
        try
        {
            if (_cacheHandler != null && (sizeHeader = _cacheHandler.GetHeader(AudioFileFetch.HEADER_SIZE)) != null)
            {

                Size = BitConverter.ToInt32(BitConverter.IsLittleEndian
                    ? sizeHeader.Take(4).Reverse().ToArray()
                    : sizeHeader.Take(4).ToArray(), 0) * 4;
                chunks = (Size + CHUNK_SIZE - 1) / CHUNK_SIZE;

                try
                {
                    firstChunk = _cacheHandler.ReadChunk(0);
                    fromCache = true;
                }
                catch (Exception x)
                {
                    switch (x)
                    {
                        case FileNotFoundException _:
                        case IOException _:
                            S_Log.Instance.LogError(x, "Failed getting first chunk from cache.");
                            var resp = AsyncContext.Run(async () => await Request(0, CHUNK_SIZE - 1));
                            firstChunk = resp.Buffer;
                            fromCache = false;
                            break;
                        default:
                            throw;
                    }
                }
            }
            else
            {
                var resp = AsyncContext.Run(async () => await Request(0, CHUNK_SIZE - 1));
                var contentRange = resp.Headers.FirstOrDefault(a => a.Name == "Content-Range").Value;
                if (contentRange == null)
                    throw new IOException("Missing Content-Range header!");

                var split = contentRange.Split('/');
                Size = int.Parse(split[1]);
                chunks = (Size + CHUNK_SIZE - 1) / CHUNK_SIZE;

                using var memStream = new MemoryStream(4);
                var getBytes = (Size / 4).ToByteArray();
                memStream.Write(getBytes,0, getBytes.Length);
                _cacheHandler?.SetHeader(AudioFileFetch.HEADER_SIZE, memStream.ToArray());

                firstChunk = resp.Buffer;
                fromCache = false;
            }

            _internalStream = new InternalCdnStream(_spotifyClient.Config.RetryOnChunkError, chunks, _cacheHandler,
                this, Size, name, haltListener);

            AsyncContext.Run(() => WriteChunk(firstChunk, 0, fromCache));
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
            Debugger.Break();
        }
    }
    public override string ToString()
    {
        return $"{{fileId: {FileId.BytesToHex()}}}";
    }
    private Task<InternalCdnChunkResponse> Request(int i, CancellationToken cancellationToken = default)
    {
        return Request(CHUNK_SIZE * i, (i + 1) * CHUNK_SIZE - 1, cancellationToken);
    }

    private async Task<InternalCdnChunkResponse> Request(int rangeStart, int rangeEnd,
        CancellationToken cancellationToken = default)
    {
        var url = await _cdnUrl.Url();
        var response = await url.WithHeader("Range", $"bytes={rangeStart}-{rangeEnd}")
            .WithOAuthBearerToken((await _spotifyClient.BearerClient.GetBearerTokenAsync(cancellationToken)))
            .GetAsync(cancellationToken: cancellationToken);

        return new InternalCdnChunkResponse(await response.GetBytesAsync(), response.Headers);
    }

    public int Size { get; }

    public AbsChunkedInputStream Stream => _internalStream;
    public SuperAudioFormat Codec => _format;
    public int DecodedLength => Stream.decodedLength;

    public int DecryptTimeMs()
    {
        return _audioDecrypt.DecryptTimeMs;
    }


    public async Task WriteChunk(byte[] chunk, int chunkIndex, bool cached)
    {
        if (_internalStream.Closed) return;

        if (!cached && _cacheHandler != null) {
            try {
               await _cacheHandler.WriteChunk(chunk, chunkIndex);
            } catch (Exception ex) {
                S_Log.Instance.LogWarning(ex, $"Failed writing to cache! {chunkIndex}");
            }
        }

        S_Log.Instance.LogInfo($"Chunk {chunkIndex}/{_internalStream.Chunks} completed, cached: {cached}, stream: {this}");

        _internalStream.Buffer[chunkIndex] = chunk;
        _audioDecrypt.DecryptChunk(chunkIndex, chunk);
        _internalStream.NotifyChunkAvailable(chunkIndex);
    }

    public async Task RequestChunk(int index,
        CancellationToken ct = default)
    {
        if (_cacheHandler != null)
        {
            try
            {
                if (_cacheHandler.HasChunk(index))
                {
                    await _cacheHandler.ReadChunk(index, this);
                    return;
                }
            }
            catch (Exception e)
            {
                S_Log.Instance.LogError($"Failed requesting chunk from cache, index: {index}", e);
            }
        }

        try
        {
            var resp = await Request(index, ct);
            await WriteChunk(resp.Buffer, index, false);
        }
        catch (Exception ex)
        {
            S_Log.Instance.LogError($"Failed requesting chunk from network, index: {index}", ex);
            _internalStream.NotifyChunkError(index, new ChunkException(ex));
        }
    }

    public void Dispose()
    {
        _internalStream.Dispose();
    }
}

internal record InternalCdnChunkResponse(byte[] Buffer, IReadOnlyNameValueList<string> Headers);

public class InternalCdnStream : AbsChunkedInputStream
{
    private int _chunks;
    private ICacheHandler _cacheHandler;
    private CdnAudioStreamer _root;
    private readonly long _size;
    private IHaltListener? _haltListener;

    private readonly string _name;
    public InternalCdnStream(bool configRetryOnChunkError, int chunks, ICacheHandler cacheHandler, 
        CdnAudioStreamer root, long size, string name, IHaltListener? haltListener = null) : base(configRetryOnChunkError)
    {
        _chunks = chunks;
        _cacheHandler = cacheHandler;
        _root = root;
        _size = size;
        _name = name;
        _haltListener = haltListener;
        AvailableChunks = new bool[_chunks];
        RequestedChunks = new Boolean[_chunks];
        Buffer = new byte[_chunks][];
        _retries = new int[_chunks];

        RequestedChunks[0] = true;
    }

    public override string ToString()
    {
        return $"{{fileId: {_root.FileId.BytesToHex()}}}";
    }

    
    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override async Task<int> ReadAsync(byte[] b, int off, int len, CancellationToken cancellationToken)
    {
        if (closed) throw new IOException("Stream is closed!");
        
        if (off < 0 || len < 0 || len > b.Length - off)
        {
            throw new ArgumentOutOfRangeException();
        }
        else if (len == 0)
        {
            return 0;
        }
        
        if (pos >= Length)
            return -1;
        
        //int i = 0;
        
        int chunk = (int) Math.Floor(pos / (double) AesAudioDecrypt.CHUNK_SIZE);
        int chunkOff = (int) (pos % AesAudioDecrypt.CHUNK_SIZE);
        
        await CheckAvailabilityAsync(chunk, true, false, cancellationToken);
        
        int copy = Math.Min((Buffer[chunk].Length) - (chunkOff), len);
        Array.Copy(Buffer[chunk], chunkOff,
            b, off, copy);
        pos += copy;
        return copy;
    }

    public override int Read(byte[] b, int off, int len)
    {
        if (closed)
            return 0;
        
        if (off < 0 || len < 0 || len > b.Length - off)
        {
            throw new ArgumentOutOfRangeException();
        }
        else if (len == 0)
        {
            return 0;
        }
        
        if (pos >= Length)
            return -1;
        
        //int i = 0;
        
        int chunk = (int) Math.Floor(pos / (double) AesAudioDecrypt.CHUNK_SIZE);
        int chunkOff = (int) (pos % AesAudioDecrypt.CHUNK_SIZE);
        
        CheckAvailability(chunk, true, false);
        if (Buffer[chunk] == null) 
            return 0;
        int copy = Math.Min((Buffer[chunk].Length) - (chunkOff), len);
        Array.Copy(Buffer[chunk], chunkOff,
            b, off, copy);
        pos += copy;
        return copy;
    }
    // public override int Read(byte[] b, int off, int len)
    // {
    //     if (closed) throw new IOException("Stream is closed!");
    //
    //     if (off < 0 || len < 0 || len > b.Length - off)
    //     {
    //         throw new ArgumentOutOfRangeException();
    //     }
    //     else if (len == 0)
    //     {
    //         return 0;
    //     }
    //
    //     if (pos >= Length)
    //         return -1;
    //
    //     //int i = 0;
    //
    //     int chunk = (int) Math.Floor(pos / (double) AesAudioDecrypt.CHUNK_SIZE);
    //     int chunkOff = (int) (pos % AesAudioDecrypt.CHUNK_SIZE);
    //
    //     CheckAvailability(chunk, true, false);
    //
    //     int copy = Math.Min((Buffer[chunk].Length / 2) - (chunkOff / 2), len);
    //     Array.Copy(Buffer[chunk], chunkOff,
    //         b, off, copy);
    //     pos += copy;
    //     return copy;
    // }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _size;
    public override string TrackName => _name;
    public override int Chunks => _chunks;
    protected override bool[] RequestedChunks { get; }
    protected override bool[] AvailableChunks { get;  }
    public override byte[][]? Buffer { get; set; }
    protected override void RequestChunkFromStream(int index)
    {
        AsyncContext.Run(() => _root.RequestChunk(index));
    }

    protected override async Task RequestChunkFromStreamAsync(int index, CancellationToken ct = default)
    {
        await _root.RequestChunk(index, ct);
    }

    public override void StreamReadHalted(int chunk, long time)
    {
        if (_haltListener != null) AsyncContext.Run(() => _haltListener.StreamReadHalted(chunk, time));

    }

    public override void StreamReadResumed(int chunk, long time)
    {
        if (_haltListener != null) AsyncContext.Run(() => _haltListener.StreamReadResumed(chunk, time));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _root = null;
        if (Buffer != null)
        {
            for (var index = 0; index < Buffer.Length; index++)
            {
                Buffer[index] = null;
            }

            Buffer = null;
        }

        _cacheHandler?.Dispose();
        _cacheHandler = null;
        _haltListener = null;

    }
}

public interface IChunkFetcher
{
    Task<byte[]> RequestChunk(int index);
}