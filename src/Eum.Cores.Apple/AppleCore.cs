using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Eum.Core.Contracts;
using Eum.Core.Models;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Clients;
using Eum.Cores.Apple.Models;
using Eum.Cores.Apple.Models.Request;
using Eum.Cores.Apple.Services;
using Microsoft.Extensions.Options;

namespace Eum.Cores.Apple;

public sealed class AppleCore : IAppleCore
{
    private readonly IClientsProvider _clientsProvider;
    private readonly IDeveloperTokenService _developerTokenService;
    private readonly IMediaTokenService _mediaTokenService;
    public AppleCore(
        IDeveloperTokenService developerTokenService,
        IMediaTokenService mediaTokenService, 
        IClientsProvider clientsProvider,
        IStoreFrontProvider storeFrontProvider)
    {
        _developerTokenService = developerTokenService;
        _mediaTokenService = mediaTokenService;
        _clientsProvider = clientsProvider;
        StoreFrontProvider = storeFrontProvider;
    }

    public static IAppleCore
        WithMediaToken(DeveloperTokenConfiguration tokenConfiguration,
            IMediaTokenOAuthHandler oauthHandler,
            string region)
    {
        IDeveloperTokenService defaultDeveloperTokenService =
            new SecretKeyDeveloperTokenService(new OptionsWrapper<DeveloperTokenConfiguration>(tokenConfiguration));
        var defaultMediaTokenService =
            new MediaTokenService(defaultDeveloperTokenService,
                oauthHandler);

        var storeFrontProvider = new ConstStorefrontProvider(region);
        return new AppleCore(defaultDeveloperTokenService, defaultMediaTokenService, 
            new RefitClientsProvider(defaultDeveloperTokenService,
                defaultMediaTokenService,
                storeFrontProvider),
            storeFrontProvider);
    }

    public static IAppleCore WithDeveloperTokenOnly(DeveloperTokenConfiguration configuration)
    {
        IDeveloperTokenService defaultDeveloperTokenService =
            new SecretKeyDeveloperTokenService(new OptionsWrapper<DeveloperTokenConfiguration>(configuration));
        var storeFrontProvider = new ConstStorefrontProvider(configuration.DefaultStorefrontId ?? 
                                                             throw new InvalidOperationException("DefaultStoreFrontId cannot be null"));

        return new AppleCore(defaultDeveloperTokenService, new EmptyMediaTokenService(),
            new RefitClientsProvider(defaultDeveloperTokenService,
                null,
                storeFrontProvider), storeFrontProvider);
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


       if (await ValidateDeveloperTokenAsync(getDeveloperToken.TokenValue, ct))
           _tokens[TokenType.DeveloperToken] = getDeveloperToken;
       else return false;

       var mediaToken = _tokens.ContainsKey(TokenType.MediaToken)
                        && _tokens[TokenType.MediaToken].HasExpired == false
           ? _tokens[TokenType.MediaToken]
           : await _mediaTokenService.GetMediaTokenAsync(ct);
       if (mediaToken != null)
       {
           if (await ValidateMediaTokenAsync(getDeveloperToken.TokenValue, ct))
               _tokens[TokenType.MediaToken] = mediaToken;
       }
       return true;
    }

    public async Task<IArtist> GetArtistAsync(string id,
        CancellationToken ct = default)
    {
        var getArtist = 
            await ArtistsClient.GetArtistAsync(id, ct);
        return getArtist.Data.FirstOrDefault() ?? throw new InvalidOperationException();
    }

    public async Task<CoreSearchedResponse> SearchAsync(string query, CancellationToken ct = default)
    {
        var searchDataAsync = await SearchClient
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
    public IArtists ArtistsClient => _clientsProvider.ArtistClient;
    public IStoreFronts StoreFrontClient => _clientsProvider.StoreFronts;
    public ISearch SearchClient => _clientsProvider.SearchClient;

    private async Task<bool> ValidateDeveloperTokenAsync(string developerToken, CancellationToken ct = default)
    {
        //TODO
        return true;
    }

    private async Task<bool> ValidateMediaTokenAsync(string accessToken, CancellationToken ct = default)
    {
        //TODO
        return true;
    }
    private readonly ConcurrentDictionary<TokenType, TokenData> _tokens = new();
}

public sealed class TokenData
{
    [Required]
    public string TokenValue { get; init; } = null!;

    public DateTimeOffset ExpiresAt { get; init; }

    public bool HasExpired => DateTimeOffset.Now >= ExpiresAt;
}
