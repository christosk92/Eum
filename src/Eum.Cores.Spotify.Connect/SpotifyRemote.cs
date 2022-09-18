using Connectstate;
using Eum.Cores.Spotify.Connect.Factories;
using Eum.Cores.Spotify.Connect.Services;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.CoreConnection;

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
    public static ISpotifyRemote Create(ISpotifyCore core)
    {
        return new SpotifyRemote(core,
            new SpotifyRemoteConnectionProvider(new SpotifyRemoteConnectionFactory(),
                new ApResolver(new HttpClient()), core.BearerClient));
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