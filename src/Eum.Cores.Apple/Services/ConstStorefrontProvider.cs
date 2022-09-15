using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Models;

namespace Eum.Cores.Apple.Services;

internal sealed class ConstStorefrontProvider : IStoreFrontProvider
{
    private readonly string _region;
    public ConstStorefrontProvider(string region)
    {
        _region = region.ToLower();
    }

    public void SetStoreFront(StoreFrontObject storeFront)
        => throw new InvalidOperationException();

    public ValueTask<string> GetConfiguredStoreFront(CancellationToken ct = default)
        => new ValueTask<string>(_region);
}