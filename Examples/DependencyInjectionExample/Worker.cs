using System.Diagnostics;
using Eum.Core;
using Eum.Core.Contracts;
using Eum.Cores.Apple;
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
            .SearchAsync("jokjae", stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
        
        
        
        
        
        
        
    }
}