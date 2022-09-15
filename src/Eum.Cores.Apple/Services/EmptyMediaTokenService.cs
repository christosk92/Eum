using Eum.Cores.Apple.Contracts;

namespace Eum.Cores.Apple.Services;

public sealed class EmptyMediaTokenService : IMediaTokenService
{
    public ValueTask<TokenData?> GetMediaTokenAsync(CancellationToken ct = default)
    {
        return new ValueTask<TokenData?>(null as TokenData);
    }
}