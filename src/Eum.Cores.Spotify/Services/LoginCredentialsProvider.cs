using CPlayerLib;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Google.Protobuf;

namespace Eum.Cores.Spotify.Services;

internal sealed class LoginCredentialsProvider : ILoginCredentialsProvider
{
    private LoginCredentials _loginCredentials;
    public LoginCredentialsProvider(string username, string password)
    {
        _loginCredentials = new LoginCredentials
        {
            Username = username,
            Typ = AuthenticationType.AuthenticationUserPass,
            AuthData = ByteString.CopyFromUtf8(password)
        };
    }
    public LoginCredentialsProvider(string reusableUsername, string reusableAuthCredentialsBase64,
        AuthenticationType reusableAuthenticationType)
    {
        _loginCredentials =  new LoginCredentials
        {
            Username = reusableUsername,
            Typ = reusableAuthenticationType,
            AuthData = ByteString.FromBase64(reusableAuthCredentialsBase64)
        };
    }


    public void SetLoginCredentials(LoginCredentials credentials)
    {
        _loginCredentials = credentials;
    }

    public LoginCredentials GetCredentials() => _loginCredentials;
}