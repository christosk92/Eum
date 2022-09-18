using System.Net;
using System.Text.Json;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Services;
using Eum.Cores.Spotify.Helpers;
using Nito.AsyncEx;
using Refit;

namespace Eum.Cores.Spotify.Services;

public sealed class RefitClientsProvider : ISpotifyClientsProvider
{
    private readonly AuthHeaderHandler _defaulthandler;
    private readonly RefitSettings _defaultRefitSettings;
    private readonly IApResolver _apResolver;
    private ISpClient? _spClient;
    private readonly AsyncLock _clientLock = new AsyncLock();
    public RefitClientsProvider(
        ISpotifyBearerService spotifyBearerService,
        IApResolver apResolver)
    {
        _apResolver = apResolver;
        _defaulthandler = new AuthHeaderHandler(spotifyBearerService, 
            new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip
        });
        
//        var baseSpClient = new HttpClient(new SetBaseUrlHandler(UrlType.SpClient, apResolver, defaultHandler));
        _defaultRefitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
        };
    }
    public async ValueTask<ISpClient> SpClient(CancellationToken ct = default)
    {
        using (_clientLock.Lock(ct))
        {
            if (_spClient != null) return _spClient;

            _spClient = RestService.For<ISpClient>(new HttpClient(_defaulthandler)
            {
                BaseAddress = new Uri(await _apResolver.GetClosestSpClient(ct))
            });
            return _spClient;
        }
    }
}