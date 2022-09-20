using System.Diagnostics;
using Eum.Core;
using Eum.Core.Contracts;
using Eum.Core.Models;
using Eum.Cores.Apple;
using Eum.Cores.Apple.Contracts.Models.Response.Search;
using Eum.Cores.Apple.Models;
using Eum.Cores.Apple.MSEdge;
using Eum.Cores.Spotify;
using Eum.Cores.Spotify.Connect.Helpers;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Contracts.Models;

namespace DependencyInjectionExample;

public sealed class Worker : BackgroundService
{
    private readonly IMergedCore _mergedCore;
    private readonly ISpotifyRemote _spotifyRemote;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, IMergedCore mergedCore, ISpotifyRemote spotifyRemote)
    {
        _logger = logger;
        _mergedCore = mergedCore;
        _spotifyRemote = spotifyRemote;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var clusterHelper = new ClusterMiddleware(_spotifyRemote);
        clusterHelper.CurrentyPlayingChanged
        
        clusterHelper.Connect();
        _spotifyRemote.ClusterUpdated += (sender, update) =>
        {
            
        };

        _spotifyRemote.Disconnected += (sender, args) =>
        {
            Task.Run(async () => await sender.ReconnectAsync(stoppingToken), stoppingToken);
        };
        
        var currentlyPlaying = await 
            _spotifyRemote
                .GetCurrentlyPlayingAsync(stoppingToken);
        var ts = currentlyPlaying.Position;
        
        
        // var searchExample = await _mergedCore
        //     .SearchAsync("jokjae",  ct: stoppingToken);
        //
        // if (searchExample[CoreType.Apple] is CoreSearchedResponse searchResponse)
        // {
        //     var artist = searchResponse
        //         .Artists.Data.First().Id;
        //
        //     var appleMusicId = new CoreId
        //     {
        //         Id = artist,
        //         Type = CoreType.Apple
        //     };
        //     //https://open.spotify.com/artist/7bWYN0sHvyH7yv1uefX07U?si=f2e289118ca64883
        //     _mergedCore.MergeIds_AndForget(appleMusicId, new CoreId
        //     {
        //         Id = "7bWYN0sHvyH7yv1uefX07U",
        //         Type = CoreType.Spotify
        //     });
        //
        //     var getArtist = await _mergedCore.GetArtist(appleMusicId, ct: stoppingToken);
        //     
        // }
        //
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}