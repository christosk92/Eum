using System.Diagnostics;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Services;
using Eum.Cores.Spotify.Factories;
using Eum.Cores.Spotify.Helpers;
using Eum.Cores.Spotify.Models;
using Nito.AsyncEx;

namespace Eum.Cores.Spotify.Services;

public sealed class MercuryBearerService : ISpotifyBearerService
{
    private readonly IMercuryUrlProvider _mercuryUrlProvider;
    private readonly ISpotifyConnectionProvider _spotifyConnectionProvider;
    public MercuryBearerService(ISpotifyConnectionProvider spotifyConnectionProvider, 
        IMercuryUrlProvider mercuryUrlProvider)
    {
        _spotifyConnectionProvider = spotifyConnectionProvider;
        _mercuryUrlProvider = mercuryUrlProvider;
    }

    public async ValueTask<string> GetBearerTokenAsync(CancellationToken ct = default)
    {
        using (await _tokenLock.LockAsync(ct))
        {
            if (_previousBearer is
                {
                    Expired: false
                })
                return _previousBearer.AccessToken;

            var connection = await _spotifyConnectionProvider.GetConnectionAsync(ct);

            
            var url = _mercuryUrlProvider.BearerTokenUrl(new []
            {
                "playlist-read"
            }, Consts.KEYMASTER_CLIENT_ID);

            var newToken = await connection.SendAndReceiveAsJson<MercuryToken>(
                url, ct: ct);
            
            var newToken_2 = await connection.SendAndReceiveAsJson<MercuryToken>(
                url, ct: ct);
      
            
            _previousBearer = newToken;
            return newToken.AccessToken;
        }
    }

    private readonly AsyncLock _tokenLock = new AsyncLock();
    private MercuryToken? _previousBearer;
}