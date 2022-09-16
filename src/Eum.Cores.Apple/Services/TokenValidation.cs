using System.Net;
using Eum.Cores.Apple.Contracts;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Eum.Cores.Apple.Services
{
    public sealed class TokenValidation : ITokenValidation
    {
        private readonly HttpClient _httpClient;
        public TokenValidation(HttpClient httpClientFactory)
        {
            _httpClient = httpClientFactory;
        }
        public Task<bool> ValidateMediaTokenAsync(
            string developerToken,
            string accessToken, CancellationToken ct = default)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", 
                developerToken);
            _httpClient.DefaultRequestHeaders.Add("media-user-token", accessToken);

            return Task.FromResult(true);
        }

        public async Task<bool> ValidateDeveloperTokenAsync(string developerToken, CancellationToken ct = default)
        {      
            //https://api.music.apple.com/v1/storefronts
            _httpClient.DefaultRequestHeaders.Authorization = new
                AuthenticationHeaderValue("Bearer", developerToken);
            try
            {
                var t = 
                    await _httpClient.GetAsync("https://api.music.apple.com/v1/storefronts", ct);
                t.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException requestException)
            {
                if (requestException.StatusCode == HttpStatusCode.Forbidden ||
                    requestException.StatusCode == HttpStatusCode.Unauthorized)
                    return false;
                throw;
            }
        }
    }
}
