using Google.Protobuf;

namespace Eum.Connections.Spotify.Cache;

public interface ICacheManager
{
    ICacheHandler GetHandler(ByteString? trackId = null, ByteString? episodeId = null);
}