using Eum.Core;
using Eum.Core.Contracts;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Models;
using Eum.Cores.Apple.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Eum.Cores.Apple;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppleMusicCore(this IServiceCollection services,
        DeveloperTokenConfiguration developerTokenConfiguration,
        IMediaTokenOAuthHandler? mediaTokenOAuthHandler = null)
    {
        services.TryAddSingleton<IUnifiedIdsConfiguration, GenericUnifiedIdsConfiguration>();

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

        if (mediaTokenOAuthHandler != null)
        {
            services.AddSingleton(mediaTokenOAuthHandler);
            services.AddTransient<IMediaTokenService, MediaTokenService>();
        }
        else
            services.AddTransient<IMediaTokenService, EmptyMediaTokenService>();

        services.AddSingleton<IClientsProvider, RefitClientsProvider>();

        services.AddSingleton<IAppleCore, AppleCore>();
        services.AddTransient<IMusicCore>(provider => provider.GetService<IAppleCore>()!);
        services.AddSingleton<IStoreFrontProvider, LazyStoreFrontsProvider>();

        return services;
    }
}