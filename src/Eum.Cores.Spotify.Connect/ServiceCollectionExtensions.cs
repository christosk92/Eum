using Eum.Cores.Spotify.Connect.Factories;
using Eum.Cores.Spotify.Contracts.Connect;
using Microsoft.Extensions.DependencyInjection;

namespace Eum.Cores.Spotify.Connect;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpotifyRemote(this IServiceCollection services)
    {
        services.AddSingleton<ISpotifyRemoteConnectionProvider,
            SpotifyRemoteConnectionProvider>();
        services.AddTransient<ISpotifyRemoteConnectionFactory,
            SpotifyRemoteConnectionFactory>();

        services.AddSingleton<ISpotifyRemote, SpotifyRemote>();
        
        return services;
    }
}