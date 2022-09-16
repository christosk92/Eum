using Eum.Core;
using Eum.Core.Contracts;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Factory;
using Eum.Cores.Apple.Models;
using Eum.Cores.Apple.MSEdge;
using Eum.Cores.Apple.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Eum.Cores.Apple;

public static class ServiceCollectionExtensions
{
    public static IAppleMusicCoreBuilderWithOAuthHandler AddAppleMusicCore(this IServiceCollection services,
        DeveloperTokenConfiguration developerTokenConfiguration)
    {
        services.TryAddSingleton<IUnifiedIdsConfiguration, GenericUnifiedIdsConfiguration>();
        services.AddHttpClient();
        services.AddOptions<DeveloperTokenConfiguration>()
            .Configure(configuration =>
            {
                configuration.KeyId = developerTokenConfiguration.KeyId;
                configuration.TeamId = developerTokenConfiguration.TeamId;
                configuration.PathToFile = developerTokenConfiguration.PathToFile;
                configuration.DefaultStorefrontId = developerTokenConfiguration.DefaultStorefrontId;
            });

        services.AddSingleton<IDeveloperTokenService,
            SecretKeyDeveloperTokenService>();

        services.AddSingleton<IClientsProvider, RefitClientsProvider>();

        services.AddSingleton<IMediaTokenService, MediaTokenService>();
        services.AddSingleton<IAppleCore, AppleCore>();
        services.AddTransient<ISecretTokenFileProvider, DefaultFileStreamTokenProvider>();
        services.AddTransient<IMusicCore>(provider => provider.GetService<IAppleCore>()!);
        services.AddSingleton<IStoreFrontProvider, LazyStoreFrontsProvider>();
        services.AddTransient<ITokenValidationFactory, TokenValidationFactory>();
        
        return new T2
        {
            Collection = services
        };
    }
}

public interface IAppleMusicCoreBuilderWithOAuthHandler
{
    IServiceCollection WithMSEdge();

    IServiceCollection WithMediaToken(string mediaToken);

}

internal struct T2 : IAppleMusicCoreBuilderWithOAuthHandler
{
    public IServiceCollection Collection { get; init; }
    
    public IServiceCollection WithMSEdge()
    {
        Collection.AddTransient<IMediaTokenOAuthHandler>(_ => new EdgeAuthHandler());
        return Collection;
    }
    public IServiceCollection WithMediaToken(
        string mediaToken)
    {
        Collection.AddTransient<IMediaTokenOAuthHandler>(_ => new StaticMediaTokenHandler(mediaToken));
        return Collection;
    }
}