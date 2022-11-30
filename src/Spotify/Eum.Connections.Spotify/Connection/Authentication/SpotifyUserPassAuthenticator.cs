using Eum.Spotify;
using Google.Protobuf;

namespace Eum.Connections.Spotify.Connection.Authentication;

public record SpotifyUserPassAuthenticator : ISpotifyAuthentication
{
    private readonly string _username;
    private readonly string _password;
    public SpotifyUserPassAuthenticator(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public LoginCredentials GetCredentials()
    {
        return new LoginCredentials
        {
            Username = _username,
            AuthData = ByteString.CopyFromUtf8(_password),
            Typ = AuthenticationType.AuthenticationUserPass
        };
    }
}