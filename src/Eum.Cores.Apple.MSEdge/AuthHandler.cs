using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.MSEdge;

namespace Eum.Cores.Edge;

public static class AuthHandler
{
    public static IMediaTokenOAuthHandler Edge
    {
        get { return new EdgeAuthHandler(); }
    }
}