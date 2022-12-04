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

public class ReusableAuthenticator : ISpotifyAuthentication
{
    private readonly string _authDataBase64;
    private AuthenticationType _typ;
    private readonly string _userId;
    public ReusableAuthenticator(string authDataBase64, AuthenticationType typ, string userId)
    {
        _authDataBase64 = authDataBase64;
        _typ = typ;
        _userId = userId;
    }
    public LoginCredentials GetCredentials()
    {
        return new LoginCredentials
        {
            Username = _userId,
            AuthData = ByteString.FromBase64(_authDataBase64),
            Typ = AuthenticationType.AuthenticationStoredSpotifyCredentials
        };
    }
}