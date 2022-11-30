using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Models;

namespace Eum.Cores.Apple
{
    public sealed class StaticMediaTokenHandler : IMediaTokenOAuthHandler
    {
        private readonly string _mediaToken;
        public StaticMediaTokenHandler(string mediaToken)
        {
            _mediaToken = mediaToken;
        }
        public void NavigateWebViewTo(Uri uri, Func<string, TokenData> callback)
        {
            callback(_mediaToken);
        }
    }
}
