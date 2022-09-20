using System.Net.Sockets;
using Eum.Core.Contracts;
using Eum.Core.Contracts.Models;
using Eum.Core.Models;
using Eum.Cores.Spotify.Connection;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Contracts.Services;
using Eum.Cores.Spotify.Factories;
using Eum.Cores.Spotify.Services;
using Eum.Cores.Spotify.Shared.Helpers;
using Eum.Cores.Spotify.Shared.Services;
using Microsoft.Extensions.Options;

namespace Eum.Cores.Spotify;

public sealed class SpotifyCore : ISpotifyCore
{
    private readonly ISpotifyConnectionProvider _spotifyConnectionProvider;
    public SpotifyCore(
        ISpotifyConnectionProvider spotifyConnectionProvider,
        ISpotifyBearerService bearerService,
        ISpotifyClientsProvider spotifyClientsProvider,
        IOptions<SpotifyConfig> config)
    {
        Config = config.Value;
        Config.DeviceId = Utils.RandomHexString(40).ToLower();
        _spotifyConnectionProvider = spotifyConnectionProvider;
        BearerClient = bearerService;
        ClientsProvider = spotifyClientsProvider;
    }

    public CoreType Type => CoreType.Spotify;
    public async Task<IArtist> GetArtistAsync(string id, CancellationToken ct = default)
    {
        var spotifyConnection = await _spotifyConnectionProvider.GetConnectionAsync(ct);
        return new SpotifyArtist();
    }

    public Task<CoreSearchedResponse> SearchAsync(string query, CancellationToken ct = default)
    {
        return Task.FromResult(new CoreSearchedResponse
        {
            Artists = new PaginatedWrapper<IArtist>
            {
                Data = new List<IArtist>()
            }
        });
    }

    public async Task<bool> EnsureConnectedAsync(CancellationToken ct = default)
    {
        var spotifyConnection = await _spotifyConnectionProvider.GetConnectionAsync(ct); 
        await spotifyConnection.EnsureConnectedAsync(ct);
        return true;
    }

    public ISpotifyBearerService BearerClient { get; }
    public ISpotifyClientsProvider ClientsProvider { get; }
    public SpotifyConfig Config { get; }

    public static ISpotifyCore Create(string username, string password)
    {
        var config = new OptionsWrapper<SpotifyConfig>(new SpotifyConfig());
        
        var apResolver = new ApResolver(new HttpClient());
        
        var loginCredentialsProvider = new LoginCredentialsProvider(username, password);
        var tcpConnectionFactory = new TcpConnectionFactory();
        var connectionFactory = new SpotifyConnectionFactory(apResolver,  tcpConnectionFactory, config);
        var connectionProvider = new SpotifyConnectionProvider(loginCredentialsProvider, connectionFactory);

        var bearerService = new MercuryBearerService(connectionProvider, new MercuryUrlProvider());
        return new SpotifyCore(connectionProvider,
            bearerService,
            new RefitClientsProvider(bearerService, apResolver)
            , config);
    }
}


public sealed class SpotifyArtist : IArtist
{
    public bool IsError => false;
    public string Id { get; }
    public string Name { get; }
    public IArtwork? Artwork { get; }
}