using System.Text.Json;
using Eum.Core.Contracts;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Clients;
using Eum.Cores.Apple.Helpers;
using Refit;

namespace Eum.Cores.Apple.Services;

internal sealed class RefitClientsProvider : IClientsProvider
{
    public RefitClientsProvider(IDeveloperTokenService defaultDeveloperTokenService,
        IMediaTokenService defaultMediaTokenService,
        IStoreFrontProvider storeFrontProvider)
    {
        var baseHttpClient = new HttpClient(new AuthHeaderHandler(defaultDeveloperTokenService,
            defaultMediaTokenService,
            storeFrontProvider))
        {
            BaseAddress = new Uri("https://api.music.apple.com/v1")
        };

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
        };

        ArtistClient = RestService.For<IArtists>(baseHttpClient, refitSettings);
        StoreFronts = RestService.For<IStoreFronts>(baseHttpClient, refitSettings);
        SearchClient = RestService.For<ISearch>(baseHttpClient, refitSettings);
    }

    public IArtists ArtistClient { get; }
    public IStoreFronts StoreFronts { get; }
    public ISearch SearchClient { get; }
}
