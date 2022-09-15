using System.Net.Http.Headers;
using Eum.Cores.Apple.Contracts;

namespace Eum.Cores.Apple.Helpers;
internal sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly IDeveloperTokenService _developerTokenService;
    private readonly IMediaTokenService? _mediaTokenService;
    private readonly IStoreFrontProvider _storeFrontProvider;
    public AuthHeaderHandler(IDeveloperTokenService developerTokenService, 
        IMediaTokenService? mediaTokenService, IStoreFrontProvider storeFrontProvider)
    {
        _developerTokenService = developerTokenService ?? throw new ArgumentNullException(nameof(developerTokenService));
        _mediaTokenService = mediaTokenService;
        _storeFrontProvider = storeFrontProvider;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _developerTokenService.GetDeveloperTokenAsync(cancellationToken);
        var mediaToken = _mediaTokenService != null? 
            await _mediaTokenService.GetMediaTokenAsync(cancellationToken) : null;
        //potentially refresh token here if it has expired etc.

        if (request.RequestUri.PathAndQuery.Contains("storefront_id"))
        {
            var storeFront = await _storeFrontProvider.GetConfiguredStoreFront(cancellationToken);
            request.RequestUri = new Uri(request
                .RequestUri.ToString()
                .Replace("storefront_id", storeFront));
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.TokenValue);
        if (mediaToken != null)
        {
            request.Headers.Add("media-user-token", mediaToken.TokenValue);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}