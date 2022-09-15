using Eum.Cores.Apple.Models;

namespace Eum.Cores.Apple.Contracts;

public interface IStoreFrontProvider
{
    void SetStoreFront(StoreFrontObject storeFront);
    ValueTask<string> GetConfiguredStoreFront(CancellationToken ct = default);
}