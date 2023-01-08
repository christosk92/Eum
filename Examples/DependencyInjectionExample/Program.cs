using DependencyInjectionExample;
using Eum.Core;
using Eum.Cores.Apple;
using Eum.Cores.Apple.Models;
using Eum.Cores.Spotify;
using Eum.Cores.Spotify.Connect;
using Eum.Cores.Spotify.Contracts.Models;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddAppleMusicCore(new DeveloperTokenConfiguration
            {
                DefaultStorefrontId = "us",
                KeyId = "C3CXXG89R7",
                TeamId = "QF7THUQ8VL",
                PathToFile = "authkey.p8"
            }) .WithMediaToken("Ar+0vu3vJqeAd8vSzcdjwZ9kmW5RBWy9zMuPJzJTcVzZjUlh2IX/LC/Vjsam5JFUQwRO/RdUy4TKSk5hUYw69XQaKyC/k4K4hhkXdIcnlmc+1gmCynyDYOajPHXaa//RE3qTWPbY88aXXkkyaaMYdUcuMVUvoBjgCoZHws0A6iiATGkQwE975oCn5q338XZXJD6UwtitwLObRMG88A8mrN3Dzn5hW45A1OBz8YajfX99/2vyEw==")
            .AddSpotifyCore(new SpotifyConfig
            {
                DeviceName = "Hello world"
            })
            .WithUsernamePassword("tak123chris@gmail.com", "Hyeminseo22")
            .MergeCores();

        services.AddSpotifyRemote();
        
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();