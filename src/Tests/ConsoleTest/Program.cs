using Eum.Cores.Apple;
using Eum.Cores.Apple.Models;

var newCore = AppleCore.WithDeveloperTokenOnly(new DeveloperTokenConfiguration
{
    KeyId = "WTB7MK5WGJ",
    PathToFile = "authkey.p8", 
    TeamId = "QF7THUQ8VL"
}, "kr");

var getArtist = await newCore.GetArtistAsync("1239707923");
var tet = "";