using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Eum.Cores.Spotify.Helpers;

internal static class StreamExtensions
{
    private static readonly int MAX_SKIP_BUFFER_SIZE = 2048;

    public static void ReadComplete(this Stream stream, byte[] buffer, int offset, int count)
    {
        var num = 0;
        while (num < count)
            num += stream.Read(buffer, offset + num, count - num);
    }

    public static async Task ReadCompleteAsync(this Stream stream,
        byte[] buffer, int offset, int count,
        CancellationToken ct = default)
    {
        var num = 0;
        while (num < count)
            num += await stream.ReadAsync(buffer, offset + num, count - num, ct);
    }
    public static async Task<byte[]> ReadCompleteAsync(this NetworkStream stream, 
        CancellationToken ct = default)
    {
        byte[] v = new byte[100000];
        var num = 0;
        while (stream.DataAvailable)
            num += await stream.ReadAsync(v, ct);
        return v.Take(num).ToArray();
    }

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