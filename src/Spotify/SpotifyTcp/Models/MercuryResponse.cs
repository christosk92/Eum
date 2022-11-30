using System;
using System.Collections.Generic;
using System.Linq;
using Eum.Spotify;

namespace SpotifyTcp.Models;

public readonly struct MercuryResponse
{
    public readonly string Uri;
    public readonly Memory<byte> Payload;
    public readonly int StatusCode;
    public long Sequence { get; }
    public readonly Header Header;
    public MercuryResponse(
        Header header, IReadOnlyCollection<byte[]> payload, long sequence)
    {
        Header = header;
        Sequence = sequence;
        Uri = header.Uri;
        StatusCode = header.StatusCode;
        Payload = payload.Skip(1)
            .Take(payload.Count - 1)
            .SelectMany(a=> a)
            .ToArray();
    }
}