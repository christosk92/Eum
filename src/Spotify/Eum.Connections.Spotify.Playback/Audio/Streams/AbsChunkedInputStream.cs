using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Eum.Connections.Spotify.Playback.Audio.Streams;

public abstract class AbsChunkedInputStream : Stream, IHaltListener
{
    private const int PRELOAD_AHEAD = 3;
    private const int PRELOAD_CHUNK_RETRIES = 2;
    private const int MAX_CHUNK_TRIES = 128;

    private readonly AsyncAutoResetEvent _waitLock = new AsyncAutoResetEvent();
    protected int[] _retries;
    private readonly bool _retryOnChunkError;
    private volatile int waitForChunk = -1;
    private volatile ChunkException chunkException = null;
    protected long pos = 0;
    private long mark = 0;
    protected volatile bool closed = false;
    public int decodedLength = 0;


    public abstract string TrackName { get; }
    protected AbsChunkedInputStream(bool retryOnChunkError)
    {
        _retryOnChunkError = retryOnChunkError;
    }


    public bool Closed { get; private set; }
    public abstract int Chunks { get; }
    protected abstract bool[] RequestedChunks { get; }

    protected abstract bool[] AvailableChunks { get; }
    public abstract byte[][] Buffer { get; }


    public void Mark()
    {
        mark = pos;
    }

    public override long Position
    {
        get => pos;
        set => pos = value;
    }

    public event EventHandler WroteSome;

    /// <summary>
    /// This mustn't take long!
    /// </summary>
    /// <param name="index"></param>
    protected abstract void RequestChunkFromStream(int index);

    protected abstract Task RequestChunkFromStreamAsync(int index, CancellationToken ct = default);
    public void Reset()
    {
        pos = mark;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (closed)
        {
            var name = TrackName;
            return 0;
        }
        if (offset == 0)
            offset += 0xa7;
        //offset += 0xa7;
        pos = offset;

        CheckAvailability((int) Math.Floor(pos / (double) AesAudioDecrypt.CHUNK_SIZE), false, false);
        return pos;
    }

    protected async ValueTask CheckAvailabilityAsync(int chunk, bool wait, bool halted,
        CancellationToken ct = default)
    {
        if (halted && !wait) throw new ArgumentException();

        if (!RequestedChunks[chunk])
        {
            await RequestChunkFromStreamAsync(chunk, ct);
            RequestedChunks[chunk] = true;
        }

        for (int i = chunk + 1; i <= Math.Min(Chunks - 1, chunk + PRELOAD_AHEAD); i++)
        {
            if (!RequestedChunks[i] && _retries[i] < PRELOAD_CHUNK_RETRIES)
            {
                await RequestChunkFromStreamAsync(i, ct);
                RequestedChunks[i] = true;
            }
        }

        if (wait)
        {
            if (AvailableChunks[chunk]) return;

            bool retry = false;

            if (!halted)
                StreamReadHalted(chunk, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            try
            {
                chunkException = null;
                waitForChunk = chunk;
                await _waitLock.WaitAsync(ct);

                if (closed) return;

                if (chunkException != null)
                {
                    if (ShouldRetry(chunk)) retry = true;
                    else throw chunkException;
                }
            }
            catch (OperationCanceledException ex)
            {
                throw new IOException(String.Empty, ex);
            }

            if (!retry) 
                StreamReadResumed(chunk, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            if (retry)
            {
                try
                {
                    await Task.Delay((int) (Math.Log10(_retries[chunk]) * 1000), ct);
                }
                catch (OperationCanceledException ignored)
                {
                }

                await CheckAvailabilityAsync(chunk, true, true, ct); // We must exit the synchronized block!
            }
        }
    }

    protected void CheckAvailability(int chunk, bool wait, bool halted)
    {
        if (halted && !wait) throw new ArgumentException();

        if (!RequestedChunks[chunk])
        {
            RequestChunkFromStream(chunk);
            RequestedChunks[chunk] = true;
        }

        for (int i = chunk + 1; i <= Math.Min(Chunks - 1, chunk + PRELOAD_AHEAD); i++)
        {
            if (!RequestedChunks[i] && _retries[i] < PRELOAD_CHUNK_RETRIES)
            {
                RequestChunkFromStream(i);
                RequestedChunks[i] = true;
            }
        }

        if (wait)
        {
            if (AvailableChunks[chunk]) return;

            bool retry = false;

            if (!halted) StreamReadHalted(chunk, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            try
            {
                chunkException = null;
                waitForChunk = chunk;
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                _waitLock.Wait(cts.Token);

                if (closed) return;

                if (chunkException != null)
                {
                    if (ShouldRetry(chunk)) retry = true;
                    else throw chunkException;
                }
            }
            catch (Exception ex)
            {
                retry = true;
            }

            if (!retry) StreamReadResumed(chunk, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            if (retry)
            {
                try
                {
                    Thread.Sleep((int) (Math.Log10(_retries[chunk] + 1) * 1000));
                }
                catch (OperationCanceledException ignored)
                {
                }

                CheckAvailability(chunk, true, true); // We must exit the synchronized block!
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        closed = true;
        _waitLock.Set();
        base.Dispose(disposing);
    }

    public override bool CanSeek => !closed;
    public abstract void StreamReadHalted(int chunk, long time);

    public abstract void StreamReadResumed(int chunk, long time);


    /// <summary>
    /// Should we retry fetching this chunk? MUST be called only for chunks that are needed immediately ({@code wait = true})!
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns></returns>
    private bool ShouldRetry(int chunk)
    {
        if (_retries[chunk] < 1) return true;
        if (_retries[chunk] > MAX_CHUNK_TRIES) return false;
        return !_retryOnChunkError;
    }
    
    // public override int Read(Span<byte> buffer)
    // {
    //     if (closed) throw new IOException("Stream is closed!");
    //     if (pos >= Length)
    //         return -1;
    //
    //     int i = 0;
    //     int chunk = (int) Math.Floor(pos / (double) AesAudioDecrypt.CHUNK_SIZE);
    //     int chunkOff = (int) (pos % AesAudioDecrypt.CHUNK_SIZE);
    //
    //     CheckAvailability(chunk, true, false);
    //
    //     int copy = Math.Min(Buffer[chunk].Length - chunkOff, buffer.Length);
    //     Buffer[chunk].AsSpan(chunkOff, copy).CopyTo(buffer);
    //     pos += copy;
    //     return copy;
    // }
    //
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
    //     int copy = Math.Min(Buffer[chunk].Length - chunkOff, len);
    //     Array.Copy(Buffer[chunk], chunkOff,
    //         b, off, copy);
    //     pos += copy;
    //     return copy;
    // }

    public void NotifyChunkError(int index, ChunkException ex)
    {
        AvailableChunks[index] = false;
        RequestedChunks[index] = false;
        _retries[index] += 1;


        if (index == waitForChunk && !closed)
        {
            chunkException = ex;
            waitForChunk = -1;
            _waitLock.Set();
        }
    }

    public void NotifyChunkAvailable(int index)
    {
        AvailableChunks[index] = true;
        decodedLength += Buffer[index].Length;

        if (index == waitForChunk && !closed)
        {
            waitForChunk = -1;
            _waitLock.Set();
        }
    }

/*public override int Read(Span<byte> buffer)
{
    if (closed) throw new IOException("Stream is closed!");

    if (pos >= Length)
        return -1;
    
    int chunk = (int)Math.Floor(pos / (double)AesAudioDecrypt.CHUNK_SIZE);
    CheckAvailability(chunk, true, false);

    var data = Buffer[chunk][pos++ % AesAudioDecrypt.CHUNK_SIZE] & 0xff;
    buffer
    return base.Read(buffer);
}*/
    public long Skip(long n)
    {
        if (n < 0) throw new ArgumentOutOfRangeException();

        if (closed) throw new IOException("Stream is closed.");

        var k = Length - pos;
        if (n < k) k = n;

        pos += k;

        int chunk = (int) Math.Floor(pos / (double) AesAudioDecrypt.CHUNK_SIZE);
        CheckAvailability(chunk, false, false);
        return k;
    }
}

public interface IHaltListener
{
    void StreamReadHalted(int chunk, long time);

    void StreamReadResumed(int chunk, long time);
}

public class ChunkException : Exception
{
    public ChunkException(Exception exception) : base(string.Empty, exception)
    {
    }
}