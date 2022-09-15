using System.Text;
using System.Text.Json;
using Eum.Cores.Apple.Contracts;
using Nito.AsyncEx;

namespace Eum.Cores.Apple.Services;

public sealed class MediaTokenService : IMediaTokenService
{
    private readonly IDeveloperTokenService _developerTokenService;
    private readonly IMediaTokenOAuthHandler _oAuthHandler;

    public MediaTokenService(IDeveloperTokenService developerTokenService, IMediaTokenOAuthHandler oAuthHandler)
    {
        _developerTokenService = developerTokenService;
        _oAuthHandler = oAuthHandler;
    }

    public async ValueTask<TokenData?> GetMediaTokenAsync(CancellationToken ct = default)
    {
        if (_cachedMediaToken is
            {
                HasExpired:false
            })
            return _cachedMediaToken;

        var generatedAt = DateTimeOffset.Now;
        var getAuthuri = await BuildAuthenticationUrl(ct);
        string? mediaAccessToken = null;

        var waitForToken = new AsyncManualResetEvent(false);

        _oAuthHandler.NavigateWebViewTo(getAuthuri, s =>
        {
            mediaAccessToken = s;
            waitForToken.Set();
        });

        await waitForToken.WaitAsync(ct);
        _cachedMediaToken = new TokenData
        {
            ExpiresAt = generatedAt.AddMonths(6),
            TokenValue = mediaAccessToken
        };
        return _cachedMediaToken;
    }

    private async ValueTask<Uri> BuildAuthenticationUrl(CancellationToken ct = default)
    {
        var developerToken = await _developerTokenService.GetDeveloperTokenAsync(ct);

        var btoaData = btoa(JsonSerializer.Serialize(new
        {
            thirdPartyIconURL = null as string,
            thirdPartyName = "Eum2",
            thirdPartyParameters = null as string,
            thirdPartyToken = developerToken
        }));
        var url =
            $"https://authorize.music.apple.com/woa?a={btoaData}&referrer=http%3A%2F%2Flocalhost%3A9000%2F&app=music&p=subscribe";
        return new Uri(url);
    }

    private static string btoa(string json)
    {
        byte[] bytes = Encoding.GetEncoding(28591).GetBytes(json);
        var toReturn = System.Convert.ToBase64String(bytes);
        return toReturn;
    }

    private TokenData? _cachedMediaToken;
}