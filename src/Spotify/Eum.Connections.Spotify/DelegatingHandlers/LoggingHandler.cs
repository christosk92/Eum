using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Logging;

namespace Eum.Connections.Spotify.DelegatingHandlers;

public class LoggingHandler : DelegatingHandler
{
    private readonly IBearerClient _tokensProvider;

    public LoggingHandler(HttpClientHandler innerHandler,
        IBearerClient tokensProvider) : base(innerHandler)
    {
        _tokensProvider = tokensProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        S_Log.Instance.LogInfo($"Pre-flight request {request.Method.Method.ToUpper()} {request.RequestUri} -----");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer",
                (await _tokensProvider.GetBearerTokenAsync(cancellationToken)));

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }
        catch (HttpRequestException ex)
        {
            S_Log.Instance.LogError($"Request {request.Method.Method.ToUpper()} FAILED with statuscode {ex} -----");
            throw;
        }
    }
}