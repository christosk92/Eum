using Eum.Core.Contracts.Models;

namespace Eum.Cores.Spotify.Contracts.Models;

public interface ISpotifyAudioItem : ISpotifyItem
{
    IArtwork[] CoverImages { get; init; }
    ISpotifyItem[] Descriptions { get; }
    TimeSpan Duration { get; }
    AudioType Type { get; }
}

public enum AudioType
{
    Track,
    Episode
}