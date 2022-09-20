using Eum.Cores.Apple.Contracts;

namespace Eum.Cores.Apple.Chrome;

public static partial class AuthHandler
{
    public static IMediaTokenOAuthHandler Chrome
    {
        get { return new ChromeAuthHandler(); }
    }
}