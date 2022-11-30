using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Models.Response.StoreFront;
using Eum.Cores.Apple.Exceptions;
using Eum.Cores.Apple.Models;
using Microsoft.Extensions.Options;

namespace Eum.Cores.Apple.Services;

public sealed class LazyStoreFrontsProvider : IStoreFrontProvider
{
    private StoreFrontObject _storeFrontObject;

    public LazyStoreFrontsProvider(DeveloperTokenConfiguration optinos)
    {
        if (optinos.DefaultStorefrontId != null)
        {
            _storeFrontObject = new StoreFrontObject
            {
                Id = optinos.DefaultStorefrontId
            };
        }
    }
    public void SetStoreFront(StoreFrontObject storeFront)
    {
        _storeFrontObject = storeFront;
    }

    public ValueTask<string> GetConfiguredStoreFront(CancellationToken ct = default)
    {
        if (_storeFrontObject == null) throw new StoreFrontNotConfiguredException();

        return new ValueTask<string>(_storeFrontObject.Id);
    }
}