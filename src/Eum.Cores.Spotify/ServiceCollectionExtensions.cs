using CPlayerLib;
using Eum.Core;
using Eum.Core.Contracts;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Factories;
using Eum.Cores.Spotify.Services;
using Eum.Cores.Spotify.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Eum.Cores.Spotify;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpotifyCore(this IServiceCollection services,
        SpotifyConfig config)
    {
        services.AddOptions<SpotifyConfig>()
            .Configure(a =>
            {
                a.DeviceId = Utils.RandomHexString(40).ToLower();
                a.DeviceName = config.DeviceName;
            });

        services.TryAddSingleton<IUnifiedIdsConfiguration, GenericUnifiedIdsConfiguration>();

        services.AddSingleton<ILoginCredentialsProvider, LoginCredentialsProvider>();
        services.AddSingleton<ISpotifyConnectionProvider, SpotifyConnectionProvider>();
        services.AddTransient<ISpotifyConnectionFactory, SpotifyConnectionFactory>();
        
        services.AddSingleton<ISpotifyCore, SpotifyCore>();
        services.AddTransient<IMusicCore>(provider => provider.GetService<ISpotifyCore>()!);
        return services;
    }
}