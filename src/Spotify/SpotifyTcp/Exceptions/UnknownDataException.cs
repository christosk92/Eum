using System;
using System.Diagnostics;
using Eum.Logging;

namespace SpotifyTcp.Exceptions;

public sealed class UnknownDataException : Exception
{
    public UnknownDataException(string cmd, byte[] data)
    {
        Debug.WriteLine(cmd);
        Data = data;
    }

    public UnknownDataException(byte[] data) : base($"Read unknown data, {data.Length} len")
    {
        S_Log.Instance.LogWarning($"Read unknown data, {data.Length} len");
        Debug.WriteLine($"Read unknown data, {data.Length} len");
        Data = data;
    }

    public byte[] Data { get; }
}