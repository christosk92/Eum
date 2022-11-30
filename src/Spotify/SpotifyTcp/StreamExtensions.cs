using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyTcp;

internal static class StreamExtensions
{
    private static readonly int MAX_SKIP_BUFFER_SIZE = 2048;

    public static async Task ReadCompleteAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken ct = default)
    {
        var num = 0;
        while (num < count)
            num += await stream.ReadAsync(buffer, offset + num, count - num, ct);
    }
    // public static async Task<byte[]> ReadCompleteAsync(this NetworkStream stream,
    //      int count, 
    //      CancellationToken ct = default)
    // {
    //     System.Diagnostics.Trace.Listeners.Clear();
    //     AsyncManualResetEvent finishedWriting = new(false);
    //     var fileLock = new AsyncLock();
    //     using var mem = new MemoryStream();
    //     var BUF = System.Buffers.ArrayPool<byte>.Shared.Rent(Math.Min(count,32 * 1024));    
    //     var read = 0;
    //     try
    //     {
    //         while (!ct.IsCancellationRequested
    //                && count - read > 0)
    //         {
    //             var x = await stream.ReadAsync(BUF, 0,
    //                 (int)Math.Min(BUF.Length,
    //                     count - read),
    //                 ct);
    //             read += x;
    //             
    //             var bufferCopy = BUF;
    //             var read1 = read;
    //             _ = Task.Run(async () =>
    //             {
    //                 using (await fileLock.LockAsync(ct))
    //                 {
    //                     // ReSharper disable once AccessToDisposedClosure
    //                     await mem.WriteAsync(bufferCopy, 0, x,
    //                             // ReSharper disable once AccessToDisposedClosure
    //                             ct)
    //                         .ConfigureAwait(false);
    //                     if (read1 == count)
    //                     {
    //                         finishedWriting.Set();
    //                     }
    //                 }
    //             }, ct);
    //         }
    //
    //         await finishedWriting.WaitAsync(ct);
    //         return mem.ToArray();
    //     }
    //     finally
    //     {
    //         System.Buffers.ArrayPool<byte>.Shared.Return(BUF, false);
    //     }
    //     
    // }

    // public static async Task<byte[]> ReadCompleteAsync(this NetworkStream stream, 
    //     CancellationToken ct = default)
    // {
    //     byte[] v = new byte[100000];
    //     var num = 0;
    //     while (stream.DataAvailable)
    //         num += await stream.ReadAsync(v, ct);
    //     return v.Take(num).ToArray();
    // }

    public static void Write(this MemoryStream input,
        string text)
    {
        var b = Encoding.Default.GetBytes(text);
        input.Write(b, 0, b.Length);
    }

    public static int GetShort(this MemoryStream input)
    {
        return GetShort(input.ToArray(), (int)input.Position, true);
    }

    private static short GetShort(byte[] obj0, int obj1, bool obj2)
    {
        return (short)(!obj2 ? (int)GetShortL(obj0, obj1) : (int)GetShortB(obj0, obj1));
    }

    private static short GetShortB(byte[] obj0, int obj1)
    {
        return MakeShort(obj0[obj1], obj0[obj1 + 1]);
    }

    private static short GetShortL(byte[] obj0, int obj1)
    {
        return MakeShort(obj0[obj1 + 1], obj0[obj1]);
    }

    private static short MakeShort(byte obj0, byte obj1)
    {
        return (short)(((sbyte)obj0 << 8) | ((sbyte)obj1 & byte.MaxValue));
    }

    public static int SkipBytes(this MemoryStream input, int n)
    {
        var total = 0;
        var cur = 0;

        while (total < n && (cur = (int)input.Skip(n - total)) > 0) total += cur;

        return total;
    }

    public static long Skip(this Stream input, long n)
    {
        var remaining = n;
        int nr;

        if (n <= 0) return 0;

        var size = (int)Math.Min(MAX_SKIP_BUFFER_SIZE, remaining);
        var skipBuffer = new byte[size];
        while (remaining > 0)
        {
            nr = input.Read(skipBuffer, 0, (int)Math.Min(size, remaining));
            if (nr < 0) break;

            remaining -= nr;
        }

        return n - remaining;
    }

    public static void WriteText(this Stream stream, string text)
    {
        if (!stream.CanWrite)
            throw new Exception("Stream is not writeable.");

        using (var writer = new StreamWriter(stream))
        {
            writer.Write(text);
            writer.Flush();
        }
    }

    public static string ReadText(this Stream stream)
    {
        if (!stream.CanRead)
            throw new Exception("Stream is not readable.");

        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }
}