using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Artists;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify.Clients;

public class MercuryArtistClient : IMercuryArtistClient
{
    private readonly IMercuryClient _mercuryClient;
    public MercuryArtistClient(IMercuryClient mercuryClient)
    {
        _mercuryClient = mercuryClient;
    }
    public async Task<MercuryArtist> GetArtistOverview(string artistId, string locale,
        CancellationToken ct = default)
    {
        var artist = await _mercuryClient.SendAndReceiveResponseAsync(MercuryUrlProvider.Artist(artistId, locale),
            MercuryRequestType.Get, ct);
        if(artist.StatusCode is >= 200 and < 300)
        {
            return JsonSerializer.Deserialize<MercuryArtist>(artist.Payload.Span, DefaultOptions.Default);
        }

        throw new MercuryException(artist.Payload, artist.StatusCode);
    }
}