using Eum.Core.Contracts;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Models;

namespace DependencyInjectionTest;

public class Worker : BackgroundService
{
    private readonly IAppleCore _applecore;
    private readonly IMergedCore _mergedCore;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger,
        IAppleCore applecore, IMergedCore mergedCore)
    {
        _logger = logger;
        _applecore = applecore;
        _mergedCore = mergedCore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var searchData = 
            await _mergedCore.SearchAsync("jokjae", stoppingToken);
        
        var getAppleCore = await _applecore
            .GetArtistAsync("1239707923", stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}