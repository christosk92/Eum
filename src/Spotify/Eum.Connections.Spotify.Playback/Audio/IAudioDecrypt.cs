namespace Eum.Connections.Spotify.Playback.Audio;

public interface IAudioDecrypt
{
    void DecryptChunk(int chunkIndex, byte[] buffer, int size = 0);
    int DecryptTimeMs { get; }
}