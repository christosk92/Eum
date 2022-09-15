using System.Runtime.CompilerServices;

namespace Eum.Cores.Apple.Exceptions;

public class StoreFrontNotConfiguredException : Exception
{
    public StoreFrontNotConfiguredException([CallerMemberName] string? calledBy = null)
        : base("StoreFront was not configured. Did you properly set the storefront by calling: SetStoreFront?")
    {

    }
}