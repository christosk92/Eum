using System;
using System.Collections.Generic;
using System.Text;
using Eum.Connections.Spotify.Models.Search;

namespace Eum.Connections.Spotify.Clients.Contracts
{
    public interface IMercurySearchClient
    {
        Task<FullSearchResponse> FullSearch(SearchRequest request,
            CancellationToken ct = default);
    }
}
