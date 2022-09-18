using CPlayerLib;

namespace Eum.Cores.Spotify.Contracts.Models;

public sealed class MercuryResponse
{
    public readonly string Uri;
    public readonly byte[] Payload;
    public readonly int StatusCode;
    public long Sequence { get; }
    public MercuryResponse(
        Header header, IReadOnlyCollection<byte[]> payload, long sequence)
    {
        Sequence = sequence;
        Uri = header.Uri;
        StatusCode = header.StatusCode;
        Payload = payload.Skip(1)
            .Take(payload.Count - 1)
            .SelectMany(a=> a)
            .ToArray();
    }
}