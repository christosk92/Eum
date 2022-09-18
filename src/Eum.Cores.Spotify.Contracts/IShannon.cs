namespace Eum.Cores.Spotify.Contracts;

public interface IShannon
{
    void Encrypt(byte[] buffer);
    void Decrypt(byte[] buffer);
    void DoNonce(byte[] nonce);
    int Nonce { get; set; }
    void Finish(byte[] fourBytesBuffer);
    void Key(byte[] sendKey);
}