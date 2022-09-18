using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;

namespace Eum.Cores.Spotify.Services;

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
    
    private string _resolvedSpClient;
    public async Task<string> GetClosestSpClient(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_resolvedSpClient))
            return _resolvedSpClient;
        //https://apresolve.spotify.com/?type=spclient
        var spClients = await
            _httpClient.GetFromJsonAsync<SpClients>("http://apresolve.spotify.com/?type=spclient", ct);
        _resolvedSpClient = "https://" + spClients.spclient.First();
        return _resolvedSpClient;
    }
    public readonly struct SpClients
    {
        public string[] spclient { get; init; }
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