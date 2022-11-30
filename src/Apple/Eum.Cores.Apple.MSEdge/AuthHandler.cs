using Eum.Cores.Apple.Contracts;

namespace Eum.Cores.Apple.MSEdge;

public static class AuthHandler
{
    public static IMediaTokenOAuthHandler Edge
    {
        get { return new EdgeAuthHandler(); }
    }
}