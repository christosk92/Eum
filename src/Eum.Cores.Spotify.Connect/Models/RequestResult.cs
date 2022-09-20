namespace Eum.Cores.Spotify.Connect.Models
{
    public enum RequestResult
    {
        UnknownSendCommandResult,
        Success,
        DeviceNotFound,
        ContextPlayerError,
        DeviceDisappeared,
        UpstreamError,
        DeviceDoesNotSupportCommand,
        RateLimited
    }
}