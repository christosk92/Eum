using System.Net.Http.Headers;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Services;

namespace Eum.Cores.Spotify.Helpers;

internal sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly ISpotifyBearerService _developerTokenService;
    public AuthHeaderHandler(ISpotifyBearerService developerTokenService, HttpClientHandler? handler = null)
    {
        _developerTokenService = developerTokenService;
        InnerHandler = handler ?? new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _developerTokenService.GetBearerTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class SetBaseUrlHandler : DelegatingHandler
{
    private readonly UrlType _urlType;
    private readonly IApResolver _apResolver;
    public SetBaseUrlHandler(UrlType urlType, IApResolver apResolver,
        AuthHeaderHandler handler)
    {
        _urlType = urlType;
        _apResolver = apResolver;
        InnerHandler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = await (_urlType switch
        {
            UrlType.SpClient => _apResolver.GetClosestSpClient(cancellationToken)
        });
        request.RequestUri = new Uri(url + request.RequestUri);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

public enum UrlType
{
    SpClient
}