using System.Net.Http.Json;
using Eum.Cores.Spotify.Contracts.CoreConnection;

namespace Eum.Cores.Spotify.Connect.Services;

public sealed class ApResolver : IApResolver
{
    private readonly HttpClient _httpClient;
    public ApResolver(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(string, ushort)> GetClosestAccessPoint(CancellationToken ct = default)
    {
        var accessPoints = await
            _httpClient.GetFromJsonAsync<AccessPoints>("http://apresolve.spotify.com/?type=accesspoint", ct);
        return accessPoints.accesspoint.Select(host =>
                (host.Split(':')[0], ushort.Parse(host.Split(':')[1])))
            .ToArray()
            .First();
    }
    public async Task<string> GetClosestDealerAsync(CancellationToken ct)
    {
        var accessPoints = await
            _httpClient.GetFromJsonAsync<Dealers>("http://apresolve.spotify.com/?type=dealer", ct);
        return accessPoints.dealer
            .First();
    }

    public readonly struct AccessPoints
    {
        public string[] accesspoint { get; init; }
    }
    private readonly struct Dealers
    {
        public string[] dealer { get; init; }
    }
}