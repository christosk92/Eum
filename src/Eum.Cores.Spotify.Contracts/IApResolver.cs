namespace Eum.Cores.Spotify.Contracts;

public interface IApResolver
{
    Task<(string, ushort)> GetClosestAccessPoint(CancellationToken ct = default);
}