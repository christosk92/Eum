using System;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Audio;

namespace Eum.Connections.Spotify.Cache;

public interface ICacheHandler : IDisposable
{
    byte[] GetHeader(byte id);
    byte[] ReadChunk(int i);
    void SetHeader(byte headerSize, byte[] data);
    bool HasChunk(int index);
    Task ReadChunk(int index, IGeneralWritableStream cdnAudioStreamer);
    Task WriteChunk(byte[] chunk, int chunkIndex);
}