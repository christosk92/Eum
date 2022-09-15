using Eum.Cores.Apple.Models;

namespace Eum.Cores.Apple.Contracts;
/// <summary>
/// Apple Music API requires the inclusion of a Music User Token for any requests for data specific to an Apple Music subscriber, such as to fetch content from the user’s library.
/// </summary>
public interface IMediaTokenService
{
    ValueTask<TokenData?> GetMediaTokenAsync(CancellationToken ct = default);
}