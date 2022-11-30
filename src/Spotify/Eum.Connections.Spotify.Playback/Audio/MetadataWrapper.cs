using Eum.Connections.Spotify.Models.Users;
using Eum.Enums;
using Eum.Spotify.metadata;

namespace Eum.Connections.Spotify.Playback.Audio;

public record MetadataWrapper(Track? track, Episode? episode)
{

    public SpotifyId Id => track != null ? 
        new SpotifyId(track.Gid, EntityType.Track) 
        : new SpotifyId(episode.Gid, EntityType.Episode);

    public int Duration => episode?.Duration ?? track!.Duration;
}