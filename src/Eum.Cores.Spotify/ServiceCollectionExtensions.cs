using Eum.Core;
using Eum.Core.Contracts;
using Eum.Cores.Spotify.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Eum.Cores.Spotify;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpotifyCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IUnifiedIdsConfiguration, GenericUnifiedIdsConfiguration>();
        services.AddSingleton<ISpotifyCore, SpotifyCore>();
        services.AddTransient<IMusicCore>(provider => provider.GetService<ISpotifyCore>()!);
        return services;
    }
}