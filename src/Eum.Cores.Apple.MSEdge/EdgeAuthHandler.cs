using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Models;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Edge;
using System.Text.Json;
using OpenQA.Selenium.Chrome;
using DevToolsSessionDomains = OpenQA.Selenium.DevTools.V104.DevToolsSessionDomains;
using Network = OpenQA.Selenium.DevTools.V104.Network;

namespace Eum.Cores.Apple.MSEdge
{
    public static class AuthHandler
    {
        public static IMediaTokenOAuthHandler Chrome
        {
            get { return new ChromeAuthHandler(); }
        }
    }
    public sealed class ChromeAuthHandler : IMediaTokenOAuthHandler
    {
        private ChromeDriver? driver = null;
        private DevToolsSession? devTools;
        private DevToolsSessionDomains devToolsSession;

        public async void NavigateWebViewTo(Uri uri, Func<string, TokenData> callback)
        {
            this.callback = callback;
            driver?.Dispose();
            devTools?.Dispose();
            driver = new ChromeDriver();
            devTools = driver.GetDevToolsSession();

            devToolsSession = devTools.GetVersionSpecificDomains<DevToolsSessionDomains>();
            await devToolsSession.Network.Enable(new Network.EnableCommandSettings());
            devToolsSession.Network.ResponseReceived += NetworkOnResponseReceived;

            driver.Url = uri.ToString();
        }

        private async void NetworkOnResponseReceived(object? sender, Network.ResponseReceivedEventArgs e)
        {
            if (e.Response.Url.Contains("getMusicUserToken"))
            {
                var getBody = new Network.GetResponseBodyCommandSettings
                {
                    RequestId = e.RequestId
                };
                var responseData = await devTools.SendCommand<Network.GetResponseBodyCommandSettings,Network.GetResponseBodyCommandResponse>(getBody);
                //"{\"isAppleMusicSubscriber\":\"true\",\"musicUserToken\":\"AsmPYbctfwArglzqZpJxfnjQZNO5S6T+L0nYMGlhj6uBwFINJtUAO7SW4CBlyPdtqxl0Iun9k8SPdmU8hiJgRTfo6GL7l6SAu32fC4S1fb423uKWL6iBP87CJnWN4hLMN7HmJZsme3JzhL51vZHWtx83XYDiu4ui/P2AnUjOKq0Wi+hbYpg8PVarHJVayNkyhBRrjrUBOpttL72xHjLZw9Xr3qHpFp2Lro1PeKHJbey0EC8jwQ\\u003d\\u003d\",\"cid\":\"09c25b222d4b434fb1b90b3356a35d91023\",\"itre\":\"0\",\"supported\":\"1\"}"

                var jsonData = JsonSerializer.Deserialize<MusicUserTokenData>(responseData.Body);

                callback(jsonData.musicUserToken);

                driver?.Dispose();
                devTools?.Dispose();
            }
        }

        private Func<string, TokenData> callback;
    }
    
    /// <summary>
    /// A Selenium, msedge powered authentication webview handler.
    /// This uses the V104 msedge engine. Please download the msedgedriver binary, and set the directory in the path environment variable.
    /// </summary>
    public sealed class EdgeAuthHandler : IMediaTokenOAuthHandler
    {
        private EdgeDriver? driver = null;
        private DevToolsSession? devTools;
        private DevToolsSessionDomains devToolsSession;

        public async void NavigateWebViewTo(Uri uri, Func<string, TokenData> callback)
        {
            this.callback = callback;
            driver?.Dispose();
            devTools?.Dispose();
            driver = new EdgeDriver();
            devTools = driver.GetDevToolsSession();

            devToolsSession = devTools.GetVersionSpecificDomains<DevToolsSessionDomains>();
            await devToolsSession.Network.Enable(new Network.EnableCommandSettings());
            devToolsSession.Network.ResponseReceived += NetworkOnResponseReceived;

            driver.Url = uri.ToString();
        }

        private async void NetworkOnResponseReceived(object? sender, Network.ResponseReceivedEventArgs e)
        {
            if (e.Response.Url.Contains("getMusicUserToken"))
            {
                var getBody = new Network.GetResponseBodyCommandSettings
                {
                    RequestId = e.RequestId
                };
                var responseData = await devTools.SendCommand<Network.GetResponseBodyCommandSettings,Network.GetResponseBodyCommandResponse>(getBody);
                //"{\"isAppleMusicSubscriber\":\"true\",\"musicUserToken\":\"AsmPYbctfwArglzqZpJxfnjQZNO5S6T+L0nYMGlhj6uBwFINJtUAO7SW4CBlyPdtqxl0Iun9k8SPdmU8hiJgRTfo6GL7l6SAu32fC4S1fb423uKWL6iBP87CJnWN4hLMN7HmJZsme3JzhL51vZHWtx83XYDiu4ui/P2AnUjOKq0Wi+hbYpg8PVarHJVayNkyhBRrjrUBOpttL72xHjLZw9Xr3qHpFp2Lro1PeKHJbey0EC8jwQ\\u003d\\u003d\",\"cid\":\"09c25b222d4b434fb1b90b3356a35d91023\",\"itre\":\"0\",\"supported\":\"1\"}"

                var jsonData = JsonSerializer.Deserialize<MusicUserTokenData>(responseData.Body);

                callback(jsonData.musicUserToken);

                driver?.Dispose();
                devTools?.Dispose();
            }
        }

        private Func<string, TokenData> callback;
    }

    internal sealed class MusicUserTokenData
    {
        public string musicUserToken { get; init; }
    }
}