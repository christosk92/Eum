using System.Threading.Tasks;
using Google.Protobuf;

namespace Eum.Connections.Spotify.Clients.Contracts;

public interface IAudioKeyManager
{
    Task<byte[]> GetAudioKey(ByteString trackGid, ByteString fileFileId, bool retry = true);
}