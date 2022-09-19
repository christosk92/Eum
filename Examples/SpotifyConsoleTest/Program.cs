
using Eum.Cores.Apple.Chrome;
using Eum.Cores.Spotify;
using Eum.Cores.Spotify.Connect;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Factories;


var core = SpotifyCore.Create("", ")";

var connect = SpotifyRemote.Create(core);

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

connect.ClusterUpdated += (sender, update) =>
{

};

var test_data = await connect.EnsureConnectedAsync();
var m = new ManualResetEvent(false);
m.WaitOne();

var t = "";