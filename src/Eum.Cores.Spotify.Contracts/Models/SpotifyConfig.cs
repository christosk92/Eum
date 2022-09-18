
using Eum.Cores.Spotify.Helpers;

namespace Eum.Cores.Spotify.Contracts.Models;

public sealed class SpotifyConfig
{
    public string DeviceName { get; set; } = "Eum";
    public string DeviceId { get; set; } = Utils.RandomHexString(40).ToLower();
}