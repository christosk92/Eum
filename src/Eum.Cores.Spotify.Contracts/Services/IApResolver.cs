namespace Eum.Cores.Spotify.Contracts.Services;

public interface IApResolver
{
    Task<(string, ushort)> GetClosestAccessPoint(CancellationToken ct = default);
    Task<string> GetClosestDealerAsync(CancellationToken ct);
    Task<string> GetClosestSpClient(CancellationToken ct);
}