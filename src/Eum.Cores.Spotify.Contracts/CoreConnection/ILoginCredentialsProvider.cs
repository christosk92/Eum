using CPlayerLib;

namespace Eum.Cores.Spotify.Contracts.CoreConnection;

public interface ILoginCredentialsProvider
{
    void SetLoginCredentials(LoginCredentials credentials);
    LoginCredentials GetCredentials();
}