
using Eum.Cores.Apple.Chrome;
using Eum.Cores.Spotify;
using Eum.Cores.Spotify.Connect;
using Eum.Cores.Spotify.Contracts.Connect;
using Eum.Cores.Spotify.Factories;


var core = SpotifyCore.Create("a", 
    "a");

var connect = SpotifyRemote.Create(core);

var test_data = await connect.EnsureConnectedAsync();
var m = new ManualResetEvent(false);
m.WaitOne();

var t = "";