
using Eum.Cores.Apple.Chrome;
using Eum.Cores.Spotify;
using Eum.Cores.Spotify.Factories;


var test = SpotifyCore.Create("tak123chris@gmail.com", 
    "Hyeminseo22");

var bearer = await test.BearerClient.GetBearerTokenAsync();


var t = "";