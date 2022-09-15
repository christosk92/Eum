namespace Eum.Cores.Apple.Contracts;

public interface IDeveloperTokenService
{
    ValueTask<TokenData> GetDeveloperTokenAsync(CancellationToken ct = default);
}