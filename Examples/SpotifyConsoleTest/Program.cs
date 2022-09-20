
using Eum.Core;
using Eum.Core.Contracts;
using Eum.Cores.Apple;
using Eum.Cores.Apple.Chrome;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Models;
using Eum.Cores.Spotify;
using Eum.Cores.Spotify.Connect;
using Eum.Cores.Spotify.Connect.Helpers;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Factories;


var spotifyCore = SpotifyCore.Create("tak123chris@gmail.com","Hyeminseo22");

var connect = SpotifyRemote.Create(spotifyCore);


connect.Disconnected += (sender, s) =>
{
    Task.Run(async () =>
    {
        bool connected = false;
        while (!connected)
        {
            connected = await sender.ReconnectAsync();
        }
    });
};

var middleWare = new ClusterMiddleware(connect);
middleWare.CurrentyPlayingChanged += (sender, s) =>
{

};
middleWare.Seeked += (sender, l) =>
{
    var timeSpan = TimeSpan.FromMilliseconds((double)l);
    Console.WriteLine($"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}");
};
middleWare.Connect();
var test_data = await connect.EnsureConnectedAsync();

var m = new ManualResetEvent(false);
m.WaitOne();

var t = "";