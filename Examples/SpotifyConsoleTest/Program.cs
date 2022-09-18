
using Eum.Cores.Spotify;
using Eum.Cores.Spotify.Factories;


var test = SpotifyCore.Create("a", "a");
var test_data = await test.EnsureConnectedAsync();

var t = "";