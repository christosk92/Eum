﻿using Connectstate;
using Eum.Cores.Spotify.Connect.Factories;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Helpers;
using Eum.Cores.Spotify.Contracts.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Websocket.Client;

namespace Eum.Cores.Spotify.Connect;

public class SpotifyRemote : ISpotifyRemote, ISpotifyRemoteReconnectOption
{
    private readonly IApResolverHttpClientProvider _apResolverHttpClientProvider;
    private readonly ISpotifyRemoteConnectionProvider _connectionProvider;
    private readonly ISpotifyCore _core;
    private ISpotifyRemoteConnection? _currentConnection;

    public SpotifyRemote(ISpotifyCore core, ISpotifyRemoteConnectionProvider connectionProvider, IApResolverHttpClientProvider apResolverHttpClientProvider)
    {
        _core = core;
        _connectionProvider = connectionProvider;
        _apResolverHttpClientProvider = apResolverHttpClientProvider;
    }
    
    //facade pattern
    public static ISpotifyRemote Create(ISpotifyCore core,
        string deviceName = "Eum-Desktop")
    {
        var spclient = AsyncContext.Run(async () => await core.ClientsProvider.SpClient());

        var httpClientProvider = new ApResolverHttpClientProvider();
        
        var apResolver = new ApResolver(httpClientProvider);
        return new SpotifyRemote(core,
            new SpotifyRemoteConnectionProvider(new SpotifyRemoteConnectionFactory(core.BearerClient, 
                    spclient, 
                    new OptionsWrapper<SpotifyConfig>(core.Config)),
                apResolver, core.BearerClient),
            httpClientProvider);
    }
    public Cluster? LatestReceivedCluster { get; }
    
    public event EventHandler<ClusterUpdate?>? ClusterUpdated;
    public event TypedEventHandler<ISpotifyRemoteReconnectOption, EventArgs>? Disconnected; 
    
    public async Task<bool> EnsureConnectedAsync(CancellationToken ct = default)
    {
        var connection =
            await _connectionProvider.GetConnectionAsync(ct);
        if (connection == null) return false;
        CurrentConnection = connection;
        return await connection.EnsureConnectedAsync(ct);
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
            _apResolverHttpClientProvider.RefreshHttpClient(); 
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
