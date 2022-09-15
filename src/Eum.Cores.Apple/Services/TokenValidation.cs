using System.Net;
using Eum.Cores.Apple.Contracts;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Eum.Cores.Apple.Services
{
    internal sealed class TokenValidation : ITokenValidation
    {
        private static HttpClient _httpClient = new HttpClient();
        public async Task<bool> ValidateMediaTokenAsync(
            string developerToken,
            string accessToken, CancellationToken ct = default)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", developerToken);
            _httpClient.DefaultRequestHeaders.Add("media-user-token", accessToken);

            return true;
        }

        public async Task<bool> ValidateDeveloperTokenAsync(string developerToken, CancellationToken ct = default)
        {    
            //https://api.music.apple.com/v1/storefronts
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", developerToken);
            try
            {
                _ =
                    await _httpClient.GetAsync("https://api.music.apple.com/v1/storefronts", ct);
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
