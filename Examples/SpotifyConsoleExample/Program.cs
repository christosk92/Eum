// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.Connections.Spotify.NAudio;
using Eum.Connections.Spotify.Playback;
using Eum.Connections.Spotify.VLC;
using Eum.Library.Logger.Helpers;
using Eum.Logging;

try
{

    string dataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Eum", "Console", "Dev"));
    var spotifyClient = SpotifyClient.Create(new SpotifyConfig
    {
        LogPath = Path.Combine(dataDir, "Logs.txt"),
        CachePath = dataDir,
        TimeSyncMethod = TimeSyncMethod.MELODY
    });
    S_Log.Instance.InitializeDefaults(Path.Combine(dataDir, "Logs.txt"), null);
    var authenticated = await
        spotifyClient.AuthenticateAsync(new SpotifyUserPassAuthenticator("tak123chris@gmail.com",
            "jkg@mbj9qvx-BPN.enc"));
// var test = await spotifyClient.MercuryClient
//     .SendAndReceiveResponseAsync(
//         "hm://context-resolve/v1/spotify:playlist:2mYemlac8Mf4rb4OqxTVmh?sessionId=1671349537118&mode=enhanced");
// var payload = Encoding.UTF8.GetString(test.Payload.Span);
    var user = await spotifyClient.Users.GetCurrentUser();
//https://open.spotify.com/track/5xrtzzzikpG3BLbo4q1Yul?si=34fafba59139480d


    var playbackClient = new SpotifyPlaybackClient(spotifyClient,
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new EumVlcPlayer() : new NAudioPlayer());
//await playbackClient.TestPlayback();
    var cluster = playbackClient.LatestCluster;
    var mn = new ManualResetEvent(false);
    mn.WaitOne();
}
catch (Exception x)
{
    S_Log.Instance.LogError(x);
}