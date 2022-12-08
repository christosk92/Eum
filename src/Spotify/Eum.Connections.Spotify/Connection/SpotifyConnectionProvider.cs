using System;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.Connections.Spotify.Models.Users;
using Nito.AsyncEx;

namespace Eum.Connections.Spotify.Connection;

public class SpotifyConnectionProvider : ISpotifyConnectionProvider
{
    private AsyncLock _connectionLock = new AsyncLock();
    private SpotifyConnectionHolder? _spotifyConnectionHolder;
    private readonly SpotifyConfig? _config;
    private ISpotifyAuthentication? _previousAuthenticator;
    public SpotifyConnectionProvider(SpotifyConfig? config)
    {
        _config = config;
    }

    public bool IsConnected => _spotifyConnectionHolder is
    {
        Connection:
        {
            IsAliveAndWell: true
        },
        AuthenticatedWithAuthenticator:
        {

        }
    };
    public AuthenticatedSpotifyUser? GetCurrentUser() => _spotifyConnectionHolder?.AuthenticatedWithUser;

    public async ValueTask<ISpotifyConnection?> GetConnectionAsync(ISpotifyAuthentication? authenticator = null,
        CancellationToken ct = default)
    {
        using (await _connectionLock.LockAsync())
        {
            if (_spotifyConnectionHolder is
                {
                    Connection:
                    {
                        IsAliveAndWell: true
                    },
                    AuthenticatedWithUser:
                    {

                    }
                })
                return _spotifyConnectionHolder.Connection;

            if (_previousAuthenticator is not null)
            {
                authenticator = _previousAuthenticator;
            }
            //ArgumentNullException.ThrowIfNull(authenticator);
            var client = new SpotifyConnection(authenticator, _config);

            var user = await client.ConnectAsync(ct);

            _previousAuthenticator = authenticator;
            _spotifyConnectionHolder = new SpotifyConnectionHolder
            {
                Connection = client,
                AuthenticatedWithUser = user,
                AuthenticatedWithAuthenticator = authenticator
            };
            return client;
        }
    }

    public void Dispose()
    {
        _spotifyConnectionHolder?.Connection?.Dispose();
    }
}

internal record SpotifyConnectionHolder
{
    public ISpotifyConnection Connection { get; init; }
    public ISpotifyAuthentication AuthenticatedWithAuthenticator { get; init; }
    public AuthenticatedSpotifyUser AuthenticatedWithUser { get; init; }
}