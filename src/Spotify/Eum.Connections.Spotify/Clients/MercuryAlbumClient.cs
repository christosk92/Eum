using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Artists;
using SpotifyTcp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Eum.Connections.Spotify.Models.Albums;

namespace Eum.Connections.Spotify.Clients
{
    public class MercuryAlbumClient : IMercuryAlbumsClient
    {
        private readonly IMercuryClient _spotifyClient;
        public MercuryAlbumClient(IMercuryClient spotifyClient)
        {
            _spotifyClient = spotifyClient;
        }

        public async Task<MercuryAlbum> GetAlbum(string albumUri, string locale, string country, CancellationToken ct = default)
        {
            var artist = await _spotifyClient
                .SendAndReceiveResponseAsync(MercuryUrlProvider.Album(albumUri, locale, country),
                MercuryRequestType.Get, ct);
            if (artist.StatusCode is >= 200 and < 300)
            {
                //var text = Encoding.UTF8.GetString(artist.Payload.ToArray());
                return JsonSerializer.Deserialize<MercuryAlbum>(artist.Payload.Span, DefaultOptions.Default);
            }

            throw new MercuryException(artist.Payload, artist.StatusCode);
        }
    }
}