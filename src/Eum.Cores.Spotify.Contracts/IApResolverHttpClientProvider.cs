namespace Eum.Cores.Spotify.Contracts;

public interface IApResolverHttpClientProvider
{
    HttpClient GetHttpClient();

    void RefreshHttpClient();
}