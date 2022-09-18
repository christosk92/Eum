﻿using System.Net.Sockets;
using Eum.Core.Contracts;
using Eum.Core.Contracts.Models;
using Eum.Core.Models;
using Eum.Cores.Spotify.Connection;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Factories;
using Eum.Cores.Spotify.Services;

namespace Eum.Cores.Spotify;

public sealed class SpotifyCore : ISpotifyCore
{
    private readonly ISpotifyConnectionProvider _spotifyConnectionProvider;

    public SpotifyCore(ISpotifyConnectionProvider spotifyConnectionProvider)
    {
        _spotifyConnectionProvider = spotifyConnectionProvider;
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

    public static ISpotifyCore Create(string username, string password)
    {
        var defaultConnectionProvider =
            new SpotifyConnectionProvider(new LoginCredentialsProvider(username, password),
                new SpotifyConnectionFactory(new ApResolver(new HttpClient()), new TcpConnectionFactory()));
        return new SpotifyCore(defaultConnectionProvider);
    }
}


public class SpotifyArtist : IArtist
{
    public bool IsError => false;
    public string Id { get; }
    public string Name { get; }
    public IArtwork? Artwork { get; }
}