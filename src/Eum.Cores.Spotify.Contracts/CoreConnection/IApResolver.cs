namespace Eum.Cores.Spotify.Contracts.CoreConnection;

public interface IApResolver
{
    Task<(string, ushort)> GetClosestAccessPoint(CancellationToken ct = default);
    Task<string> GetClosestDealerAsync(CancellationToken ct);
}