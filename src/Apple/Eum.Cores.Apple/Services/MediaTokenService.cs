using System.Text;
using System.Text.Json;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Models;
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
            _cachedMediaToken = new TokenData
            {
                ExpiresAt = generatedAt.AddMonths(6),
                TokenValue = mediaAccessToken
            };
            waitForToken.Set();
            return _cachedMediaToken;
        });

        await waitForToken.WaitAsync(ct);
        return _cachedMediaToken;
    }

    public bool IsAuthenticated => _cachedMediaToken is not null && !_cachedMediaToken.HasExpired;

    private async ValueTask<Uri> BuildAuthenticationUrl(CancellationToken ct = default)
    {
        var developerToken = await _developerTokenService.GetDeveloperTokenAsync(ct);

        var btoaData = btoa(JsonSerializer.Serialize(new
        {
            thirdPartyIconURL = null as string,
            thirdPartyName = "Eum2",
            thirdPartyParameters = null as string,
            thirdPartyToken = developerToken.TokenValue
        }));
        var url =
            $"https://authorize.music.apple.com/woa?a={btoaData}&app=music&p=subscribe";
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