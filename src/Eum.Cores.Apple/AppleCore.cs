using System.Collections.Concurrent;
using System.Security.Authentication;
using Eum.Core.Contracts;
using Eum.Core.Models;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Factory;
using Eum.Cores.Apple.Contracts.Models;
using Eum.Cores.Apple.Contracts.Models.Request;
using Eum.Cores.Apple.Models;
using Eum.Cores.Apple.MSEdge;
using Eum.Cores.Apple.Services;
using Microsoft.Extensions.Options;

namespace Eum.Cores.Apple;

public sealed class AppleCore : IAppleCore
{
    private readonly ITokenValidationFactory _bearerTokenValidatorFactory;
    private readonly IDeveloperTokenService _developerTokenService;
    private readonly IMediaTokenService _mediaTokenService;
    public AppleCore(
        IDeveloperTokenService developerTokenService,
        IMediaTokenService mediaTokenService, 
        IClientsProvider clientsProvider,
        IStoreFrontProvider storeFrontProvider, ITokenValidationFactory bearerTokenValidatorFactory)
    {
        _developerTokenService = developerTokenService;
        _mediaTokenService = mediaTokenService;
        Clients = clientsProvider;
        StoreFrontProvider = storeFrontProvider;
        _bearerTokenValidatorFactory = bearerTokenValidatorFactory;
    }
    public IClientsProvider Clients { get; }

    public static IAppleCore Create(DeveloperTokenConfiguration tokenConfiguration,
        IMediaTokenOAuthHandler webviewAuthHandler)
    {
        IDeveloperTokenService defaultDeveloperTokenService =
            new SecretKeyDeveloperTokenService(new OptionsWrapper<DeveloperTokenConfiguration>(tokenConfiguration));
        var defaultMediaTokenService =
            new MediaTokenService(defaultDeveloperTokenService,
                webviewAuthHandler);

        var lazyStorefrontProvider =
            new LazyStoreFrontsProvider(new OptionsWrapper<DeveloperTokenConfiguration>(tokenConfiguration));

        return new AppleCore(defaultDeveloperTokenService, defaultMediaTokenService,
            new RefitClientsProvider(defaultDeveloperTokenService,
                defaultMediaTokenService,
                lazyStorefrontProvider),
            lazyStorefrontProvider,
            new TokenValidationFactory(new HttpClient()));
    }


    public CoreType Type => CoreType.Apple;

    public bool IsAuthenticated
    {
        get
        {
            var developerToken = _tokens
                .FirstOrDefault(a => a.Key == TokenType.DeveloperToken)
                .Value;
            var hasExpired = developerToken?.HasExpired ?? true;
            return !hasExpired;
        }
    }
    public async ValueTask<bool> AuthenticateAsync(CancellationToken ct = default)
    {
       //The tactic is to first get a developer token, because media token requires a developer token
       //And we validate each of them individually.
       var getDeveloperToken = IsAuthenticated
           ? _tokens[TokenType.DeveloperToken]
           : await _developerTokenService.GetDeveloperTokenAsync(ct);

       var bearerTokenValidator = _bearerTokenValidatorFactory.GetTokenValidation();
       if (await bearerTokenValidator.ValidateDeveloperTokenAsync(getDeveloperToken.TokenValue, ct))
           _tokens[TokenType.DeveloperToken] = getDeveloperToken;
       else return false;

       var mediaToken = _tokens.ContainsKey(TokenType.MediaToken)
                        && _tokens[TokenType.MediaToken].HasExpired == false
           ? _tokens[TokenType.MediaToken]
           : await _mediaTokenService.GetMediaTokenAsync(ct);
       if (mediaToken != null)
       {
           if (await bearerTokenValidator.ValidateMediaTokenAsync(getDeveloperToken.TokenValue,  mediaToken.TokenValue, ct))
               _tokens[TokenType.MediaToken] = mediaToken;
       }

       return true;
    }

    public async ValueTask<bool> AuthenticateAsync(TokenData mediaAccessToken, CancellationToken ct = default)
    {
        //The tactic is to first get a developer token, because media token requires a developer token
        //And we validate each of them individually.
        var getDeveloperToken = IsAuthenticated
            ? _tokens[TokenType.DeveloperToken]
            : await _developerTokenService.GetDeveloperTokenAsync(ct);
        var bearerTokenValidator = _bearerTokenValidatorFactory.GetTokenValidation();
        if (await bearerTokenValidator.ValidateDeveloperTokenAsync(getDeveloperToken.TokenValue, ct))
            _tokens[TokenType.DeveloperToken] = getDeveloperToken;
        else return false;

        if (await bearerTokenValidator.ValidateMediaTokenAsync(getDeveloperToken.TokenValue, 
                mediaAccessToken.TokenValue, ct))
            _tokens[TokenType.MediaToken] = mediaAccessToken;
        else
        {
            var newToken 
                = await _mediaTokenService.GetMediaTokenAsync(ct);
            if (newToken != null)
            {
                if (await bearerTokenValidator.ValidateMediaTokenAsync(getDeveloperToken.TokenValue, 
                        mediaAccessToken.TokenValue, ct))
                    _tokens[TokenType.MediaToken] = newToken;
            }
            else
            {
                throw new AuthenticationException();
            }
        }
        return true;
    }

    public async Task<IArtist> GetArtistAsync(string id,
        CancellationToken ct = default)
    {
        var getArtist = 
            await Clients.ArtistClient.GetArtistAsync(id, ct);
        return getArtist.Data.FirstOrDefault() ?? throw new InvalidOperationException();
    }

    public async Task<CoreSearchedResponse> SearchAsync(string query, CancellationToken ct = default)
    {
        var searchDataAsync = await Clients.SearchClient
            .SearchCatalogue(new SearchQueryParameters
            {
                Term = query
            }, ct);

        return new CoreSearchedResponse
        {
            Artists =
                searchDataAsync.Results
                    .Artists != null
                    ? new PaginatedWrapper<IArtist>
                    {
                        Data = searchDataAsync.Results
                            .Artists.Data,
                        Next = searchDataAsync.Results.Artists.Next
                    }
                    : null
        };
    }

    public IStoreFrontProvider StoreFrontProvider { get; }
    public IReadOnlyDictionary<TokenType, TokenData> Tokens => _tokens;

    private readonly ConcurrentDictionary<TokenType, TokenData> _tokens = new();
}

public sealed class TokenValidationFactory : ITokenValidationFactory
{
    private readonly HttpClient _httpClient;
    public TokenValidationFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public ITokenValidation GetTokenValidation()
    {
        return new TokenValidation(_httpClient);
    }
}