using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Services;

namespace Eum.Cores.Spotify.Connect.Factories;

public sealed class SpotifyRemoteConnectionProvider : ISpotifyRemoteConnectionProvider
{
    private ISpotifyRemoteConnection? _previousConnection;
    private readonly IApResolver _apResolver;
    private readonly ISpotifyRemoteConnectionFactory _connectionFactory;
    private readonly ISpotifyBearerService _bearerService;
    public SpotifyRemoteConnectionProvider(ISpotifyRemoteConnectionFactory connectionFactory, 
        IApResolver apResolver, 
        ISpotifyBearerService bearerService)
    {
        _connectionFactory = connectionFactory;
        _apResolver = apResolver;
        _bearerService = bearerService;
    }
    
    public async ValueTask<ISpotifyRemoteConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_previousConnection is
            {
                IsAlive: true
            })
            return _previousConnection;

        var getWssUrl = await _apResolver.GetClosestDealerAsync(ct);
        var token = await _bearerService.GetBearerTokenAsync(ct);
        var socketUrl =
            $"wss://{getWssUrl.Replace("https://", string.Empty)}/" +
            $"?access_token={token}";
        var getconnection =
            _connectionFactory.GetConnection(socketUrl);
        
        await getconnection.EnsureConnectedAsync(ct);

        _previousConnection = getconnection;
        
        return getconnection;
    }
}