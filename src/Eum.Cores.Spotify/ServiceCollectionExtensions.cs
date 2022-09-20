using CPlayerLib;
using Eum.Core;
using Eum.Core.Contracts;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Contracts.Services;
using Eum.Cores.Spotify.Factories;
using Eum.Cores.Spotify.Services;
using Eum.Cores.Spotify.Shared.Helpers;
using Eum.Cores.Spotify.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Eum.Cores.Spotify;

public static class ServiceCollectionExtensions
{
    public interface IUnauthorizedSpotifyCore
    {
        IServiceCollection WithLazyAuthentication();

        IServiceCollection WithUsernamePassword(string username, string password);
    }
    public static IUnauthorizedSpotifyCore AddSpotifyCore(this IServiceCollection services,
        SpotifyConfig config)
    {
        services.AddOptions<SpotifyConfig>()
            .Configure(a =>
            {
                a.DeviceId = Utils.RandomHexString(40).ToLower();
                a.DeviceName = config.DeviceName;
            });

        services.TryAddSingleton<IApResolver, ApResolver>();
        services.TryAddSingleton<IUnifiedIdsConfiguration, GenericUnifiedIdsConfiguration>();

        services.AddSingleton<ISpotifyClientsProvider, RefitClientsProvider>();
        services.AddTransient<IMercuryUrlProvider, MercuryUrlProvider>();
        services.AddSingleton<ISpotifyBearerService, MercuryBearerService>();
        services.AddTransient<ITcpConnectionFactory, TcpConnectionFactory>();
        services.AddSingleton<ISpotifyConnectionProvider, SpotifyConnectionProvider>();
        services.AddTransient<ISpotifyConnectionFactory, SpotifyConnectionFactory>();
        
        services.AddSingleton<ISpotifyCore, SpotifyCore>();
        services.AddTransient<IMusicCore>(provider => provider.GetService<ISpotifyCore>()!);
        return new _T(services);
    }
    
    private readonly struct _T : IUnauthorizedSpotifyCore
    {
        private readonly IServiceCollection _services;
        public _T(IServiceCollection collection)
        {
            _services = collection;
        }
        public IServiceCollection WithLazyAuthentication()
        {
            _services.AddSingleton<ILoginCredentialsProvider, LoginCredentialsProvider>();
            return _services;
        }

        public IServiceCollection WithUsernamePassword(string username, string password)
        {
            _services.AddSingleton<ILoginCredentialsProvider>(new LoginCredentialsProvider(username, password));
            return _services;
        }
    }
    
}