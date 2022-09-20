using Connectstate;
using Eum.Cores.Spotify.Connect.Factories;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Shared;
using Eum.Cores.Spotify.Shared.Helpers;
using Eum.Cores.Spotify.Shared.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Websocket.Client;

namespace Eum.Cores.Spotify.Connect;

public sealed class SpotifyRemote : ISpotifyRemote, ISpotifyRemoteReconnectOption
{
    private readonly ISpotifyRemoteConnectionProvider _connectionProvider;
    private readonly ISpotifyCore _core;
    private ISpotifyRemoteConnection? _currentConnection;

    public SpotifyRemote(ISpotifyCore core,
        ISpotifyRemoteConnectionProvider connectionProvider)
    {
        _core = core;
        _connectionProvider = connectionProvider;
    }
    
    //facade pattern
    public static ISpotifyRemote Create(ISpotifyCore core,
        string deviceName = "Eum-Desktop")
    {
        
        var apResolver = new ApResolver(new HttpClient());
        return new SpotifyRemote(core,
            new SpotifyRemoteConnectionProvider(new SpotifyRemoteConnectionFactory(core.BearerClient, 
                    core.ClientsProvider, 
                    new OptionsWrapper<SpotifyConfig>(core.Config)),
                apResolver, core.BearerClient));
    }
    public Cluster? LatestReceivedCluster { get; }
    
    public event EventHandler<ClusterUpdate?>? ClusterUpdated;
    public event TypedEventHandler<ISpotifyRemoteReconnectOption, EventArgs>? Disconnected; 
    
    public async ValueTask<bool> EnsureConnectedAsync(CancellationToken ct = default)
    {
        var connection =
            await _connectionProvider.GetConnectionAsync(ct);
        if (connection == null) return false;
        CurrentConnection = connection;
        return await connection.EnsureConnectedAsync(ct);
    }

    public async ValueTask<CurrentlyPlayingState?> GetCurrentlyPlayingAsync(CancellationToken stoppingToken = default)
    {
        var connected = 
            await EnsureConnectedAsync(stoppingToken);
        if (!connected) return null;

        var cluster = CurrentConnection.PreviousCluster
            .PlayerState;
        return new CurrentlyPlayingState(cluster);
    }

    public ISpotifyRemoteConnection? CurrentConnection
    {
        get => _currentConnection;
        set
        {
            if (_currentConnection != null && _currentConnection.ConnectionId != value?.ConnectionId)
            {
                _currentConnection.ClusterUpdated -= CurrentConnectionOnClusterUpdated;
                _currentConnection.Disconnected -= CurrentConnectionOnDisconnected;
                _currentConnection.Dispose();
            }
            if (_currentConnection?.ConnectionId != value?.ConnectionId)
            {
                if (value != null)
                {
                    value.ClusterUpdated += CurrentConnectionOnClusterUpdated;
                    value.Disconnected += CurrentConnectionOnDisconnected;
                }
            }

            _currentConnection = value;
        }
    }

    private void CurrentConnectionOnDisconnected(object? sender, string e) => 
        Disconnected?.Invoke(this, EventArgs.Empty);

    private void CurrentConnectionOnClusterUpdated(object? sender, ClusterUpdate? e) => 
        ClusterUpdated?.Invoke(this, e);

    public async Task<bool> ReconnectAsync(CancellationToken ct = default)
    {
        try
        {
            var connected =
                await EnsureConnectedAsync(ct);
            return connected;
        }
        catch (Exception x)
        {
            return false;
        }
    }
}
