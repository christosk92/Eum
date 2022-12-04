using System;
using System.Linq;
using System.Numerics;
using Google.Protobuf;
using SpotifyTcp.Helpers;

namespace Eum.Connections.Spotify.Helpers;

public static class Utils
{
    private static readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();
    private static readonly string randomString = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static byte[] toByteArray(BigInteger i)
    {
        var array = i.ToByteArray();
        if (array[0] == 0) array = ByteExtensions.copyOfRange(array, 1, array.Length);
        return array;
    }

    public static string bytesToHex(byte[] bytes, int offset, int length, bool trim, int minLength)
    {
        if (bytes == null) return "";

        var newOffset = 0;
        var trimming = trim;
        var hexChars = new char[length * 2];
        for (var j = offset; j < length; j++)
        {
            var v = bytes[j] & 0xFF;
            if (trimming)
            {
                if (v == 0)
                {
                    newOffset = j + 1;

                    if (minLength != -1 && length - newOffset == minLength)
                        trimming = false;

                    continue;
                }

                trimming = false;
            }

            hexChars[j * 2] = hexArray[(uint)v >> 4];
            hexChars[j * 2 + 1] = hexArray[v & 0x0F];
        }

        return new string(hexChars, newOffset * 2, hexChars.Length - newOffset * 2);
    }

    public static byte[] toByteArray(int i)
    {
        var bytes = BitConverter.GetBytes(i);
        if (BitConverter.IsLittleEndian) return bytes.Reverse().ToArray();

        return bytes;
    }

    public static string RandomHexString(int length)
    {
        var bytes = new byte[length / 2];
        new Random().NextBytes(bytes);
        return BytesToHex(bytes, 0, bytes.Length, false, length);
    }

    public static string BytesToHex(this ByteString bytes)
    {
        return BytesToHex(bytes.ToByteArray());
    }

    public static string BytesToHex(this byte[] bytes)
    {
        return BytesToHex(bytes, 0, bytes.Length, false, -1);
    }

    public static string BytesToHex(byte[] bytes, int off, int len)
    {
        return BytesToHex(bytes, off, len, false, -1);
    }


    public static string BytesToHex(byte[] bytes, int offset, int length, bool trim, int minLength)
    {
        if (bytes == null) return "";

        var newOffset = 0;
        var trimming = trim;
        var hexChars = new char[length * 2];
        for (var j = offset; j < length; j++)
        {
            var v = bytes[j] & 0xFF;
            if (trimming)
            {
                if (v == 0)
                {
                    newOffset = j + 1;

                    if (minLength != -1 && length - newOffset == minLength)
                        trimming = false;

                    continue;
                }

                trimming = false;
            }

            hexChars[j * 2] = hexArray[(int)((uint)v >> 4)];
            hexChars[j * 2 + 1] = hexArray[v & 0x0F];
        }

        return new string(hexChars, newOffset * 2, hexChars.Length - newOffset * 2);
    }

    public static byte[] HexToBytes(this string str)
    {
        var len = str.Length;
        var data = new byte[len / 2];
        for (var i = 0; i < len; i += 2)
            data[i / 2] = (byte)((Convert.ToInt32(str[i].ToString(),
                16) << 4) + Convert.ToInt32(str[i + 1].ToString(), 16));
        return data;
    }
}