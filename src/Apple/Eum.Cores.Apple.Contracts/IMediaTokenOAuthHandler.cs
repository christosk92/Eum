using Eum.Cores.Apple.Contracts.Models;

namespace Eum.Cores.Apple.Contracts;


/// <summary>
/// This service must be implemented by the consumer, and it's function members will be called by the SDK. <br/>
/// The general idea is that you must set-up some kind of WebView, and intercept the GetMediaToken call. <br/>
/// Check the example projects for an example in Windows. <br/>
/// It is up to the consumer of this application to handle oauth sign in. <br/>
/// </summary>
public interface IMediaTokenOAuthHandler 
{
    /// <summary>
    /// This method will be invoked, when a webview is requested.
    /// Once a token has been received, use the method in <see cref="callback"/> to enter the media token.
    /// </summary>
    /// <param name="uri">The uri to set the webview to.</param>
    /// <param name="callback">The callback once the mediatoken has been retrieved. Input must be the mediatoken, or null if failed to retrieve.</param>
    void NavigateWebViewTo(Uri uri, Func<string, TokenData> callback);
}