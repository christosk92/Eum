using System.Net.Http.Headers;
using Eum.Cores.Spotify.Contracts.Services;

namespace Eum.Cores.Spotify.Connect.HttpHandlers;

internal class LoggingHandler : DelegatingHandler
{
    private readonly ISpotifyBearerService _tokensProvider;

    internal LoggingHandler(HttpClientHandler innerHandler,
        ISpotifyBearerService tokensProvider) : base(innerHandler)
    {
        _tokensProvider = tokensProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer",
                (await _tokensProvider.GetBearerTokenAsync(cancellationToken)));

        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}