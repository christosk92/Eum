using System.Threading.Tasks;

namespace Eum.Connections.Spotify.Audio;

public interface IGeneralWritableStream
{
    Task WriteChunk(byte[] buffer, int chunkIndex, bool cached);
}