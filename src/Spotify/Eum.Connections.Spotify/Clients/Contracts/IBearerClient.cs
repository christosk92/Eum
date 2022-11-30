using System.Threading;
using System.Threading.Tasks;

namespace Eum.Connections.Spotify.Clients.Contracts;

public interface IBearerClient
{
    ValueTask<string?> GetBearerTokenAsync(CancellationToken cancellationToken = default);
}