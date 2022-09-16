using System.Diagnostics;
using Eum.Core;
using Eum.Core.Contracts;
using Eum.Core.Models;
using Eum.Cores.Apple;
using Eum.Cores.Apple.Contracts.Models.Response.Search;
using Eum.Cores.Apple.Models;
using Eum.Cores.Apple.MSEdge;
using Eum.Cores.Spotify;

namespace DependencyInjectionExample;

public class Worker : BackgroundService
{
    private readonly IMergedCore _mergedCore;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, IMergedCore mergedCore)
    {
        _logger = logger;
        _mergedCore = mergedCore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var searchExample = await _mergedCore
            .SearchAsync("jokjae",  ct: stoppingToken);

        if (searchExample[CoreType.Apple] is CoreSearchedResponse searchResponse)
        {
            var artist = searchResponse
                .Artists.Data.First().Id;

            var appleMusicId = new CoreId
            {
                Id = artist,
                Type = CoreType.Apple
            };
            //https://open.spotify.com/artist/7bWYN0sHvyH7yv1uefX07U?si=f2e289118ca64883
            _mergedCore.MergeIds_AndForget(appleMusicId, new CoreId
            {
                Id = "7bWYN0sHvyH7yv1uefX07U",
                Type = CoreType.Spotify
            });

            var getArtist = await _mergedCore.GetArtist(appleMusicId, ct: stoppingToken);
            
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}