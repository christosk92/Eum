using CPlayerLib;
using Eum.Cores.Spotify.Contracts;

namespace Eum.Cores.Spotify.Factories;

public sealed class SpotifyTcpConnectionProvider : ISpotifyConnectionProvider
{
    private readonly ILoginCredentialsProvider _loginCredentials;
    private readonly ISpotifyConnectionFactory _spotifyConnectionFactory;
    public SpotifyTcpConnectionProvider(ILoginCredentialsProvider loginCredentialsProvider, 
        ISpotifyConnectionFactory spotifyConnectionFactory)
    {
        _loginCredentials = loginCredentialsProvider;
        _spotifyConnectionFactory = spotifyConnectionFactory;
    }
    public async Task<ISpotifyConnection> GetConnectionAsync(CancellationToken ct)
    {
        var credentials = _loginCredentials.GetCredentials();
        if (!_previousConnection.ShouldGetNewConnection(credentials))
        {
            return _previousConnection.connection;
        }
        
        var newConnection = _spotifyConnectionFactory.GetNewConnection();
        await newConnection.InstantiateConnectionAsync(ct);
        await newConnection.AuthenticateAsync(credentials, ct);
        _previousConnection = new _t
        {
            forCredentials = credentials,
            connection = newConnection
        };
        return newConnection;
    }

    private _t _previousConnection;
    private readonly record struct _t
    {
        public ISpotifyConnection connection { get; init; }
        public LoginCredentials forCredentials{ get; init; }

        public bool ShouldGetNewConnection(LoginCredentials credentials)
        {
            if (connection is
                {
                    IsAlive: true
                })
            {
                return forCredentials?.Username != credentials.Username;
            }

            return true;
        }
    }
}