namespace Eum.Cores.Spotify.Contracts;

public interface IShannon
{
    void Key(byte[] key);
    void Encrypt(byte[] buffer);
    void Decrypt(byte[] buffer);
}