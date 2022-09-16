using CPlayerLib;

namespace Eum.Cores.Spotify.Contracts;

public interface ILoginCredentialsProvider
{
    void SetLoginCredentials(LoginCredentials credentials);
    LoginCredentials GetCredentials();
}