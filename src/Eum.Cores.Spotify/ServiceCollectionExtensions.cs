using Eum.Core;
using Eum.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Eum.Cores.Spotify;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpotifyCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IUnifiedIdsConfiguration, GenericUnifiedIdsConfiguration>();

        return services;
    }
}