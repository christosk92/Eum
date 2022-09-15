using DependencyInjectionTest;
using Eum.Core;
using Eum.Cores.Apple;
using Eum.Cores.Apple.Models;
using Eum.Cores.Spotify;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();

        services.AddSpotifyCore()
            .AddAppleMusicCore(new DeveloperTokenConfiguration
            {
                KeyId = "WTB7MK5WGJ",
                PathToFile = "authkey.p8",
                TeamId = "QF7THUQ8VL",
                DefaultStorefrontId = "kr"
            })
            .MergeCores();
    })
    .Build();

host.Run();