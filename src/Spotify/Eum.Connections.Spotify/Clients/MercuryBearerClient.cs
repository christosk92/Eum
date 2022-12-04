using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Token;
using Nito.AsyncEx;

namespace Eum.Connections.Spotify.Clients;

public class MercuryBearerClient : IBearerClient
{
    internal const string KEYMASTER_CLIENT_ID = "65b708073fc0480ea92a077233ca87bd";

    private readonly IMercuryClient _mercuryClient;

    public MercuryBearerClient(IMercuryClient mercuryClient)
    {
        _mercuryClient = mercuryClient;
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

            var url = MercuryUrlProvider.Bearer(new[]
            {
                "user-read-private",
                "user-read-email",
                "playlist-modify-public",
                "ugc-image-upload",
                "playlist-read-private",
                "playlist-read-collaborative",
                "playlist-read"
            }, KEYMASTER_CLIENT_ID);

            var newTokenResponse = await _mercuryClient.SendAndReceiveResponseAsync(
                url, ct: ct);
            var newToken = JsonSerializer.Deserialize<MercuryToken>(newTokenResponse.Payload.Span);
            _previousBearer = newToken;
            return newToken.AccessToken;
        }
    }

    private static readonly AsyncLock _tokenLock = new AsyncLock();
    private static MercuryToken? _previousBearer;
}