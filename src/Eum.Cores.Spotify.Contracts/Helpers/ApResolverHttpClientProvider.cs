namespace Eum.Cores.Spotify.Contracts.Helpers;

public class ApResolverHttpClientProvider : IApResolverHttpClientProvider
{
    private HttpClient _httpClient;

    public ApResolverHttpClientProvider()
    {
        _httpClient = new HttpClient();
    }
    public HttpClient GetHttpClient()
    {
        return _httpClient;
    }

    public void RefreshHttpClient()
    {
        _httpClient?.Dispose();
        _httpClient = new HttpClient();
    }
}