using Eum.Cores.Apple.Chrome;
using Eum.Cores.Apple.Contracts;

namespace Eum.Cores.Chrome;

public static partial class AuthHandler
{
    public static IMediaTokenOAuthHandler Chrome
    {
        get { return new ChromeAuthHandler(); }
    }
}