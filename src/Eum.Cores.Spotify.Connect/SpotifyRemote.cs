using Connectstate;
using Eum.Cores.Spotify.Connect.Factories;
using Eum.Cores.Spotify.Connect.Services;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Models;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace Eum.Cores.Spotify.Connect;

public class SpotifyRemote : ISpotifyRemote
{
    private readonly ISpotifyRemoteConnectionProvider _connectionProvider;
    private readonly ISpotifyCore _core;
    public SpotifyRemote(ISpotifyCore core, ISpotifyRemoteConnectionProvider connectionProvider)
    {
        _core = core;
        _connectionProvider = connectionProvider;
    }
    
    //facade pattern
    public static ISpotifyRemote Create(ISpotifyCore core,
        string deviceName = "Eum-Desktop")
    {
        var spclient = AsyncContext.Run(async () => await core.ClientsProvider.SpClient());
        var httpclient = new HttpClient();
        return new SpotifyRemote(core,
            new SpotifyRemoteConnectionProvider(new SpotifyRemoteConnectionFactory(core.BearerClient, 
                    spclient, 
                    new OptionsWrapper<SpotifyConfig>(core.Config)),
                new ApResolver(httpclient), core.BearerClient));
    }
    public Cluster? LatestReceivedCluster { get; }
    public event EventHandler<ClusterUpdate?>? ClusterUpdated;
    
    public async Task<bool> EnsureConnectedAsync(CancellationToken ct = default)
    {
        var getConnection =
            await _connectionProvider.GetConnectionAsync(ct);
        return await getConnection.EnsureConnectedAsync(ct);
    }
}