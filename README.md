
<h1 align="center">Eum 🎵</h1>

## 🤔 Whats is this?

A merged & unified music services library for .NET Standard 2.0 and up.

For [Apple Music](src/Eum.Cores.Apple) & [Spotify](src/Eum.Cores.Spotify).


|                |Apple Music                          |Spotify                         |
|----------------|-------------------------------|-----------------------------|
|**Sign In**| ✅ Done | ✅ Done
|**Fetch basic data**|  🚧 In Progress            | ✅ Done            |
|**Playback**          | 🤔 Planned for future release            | ✅ Done*           |


* Playback is working but not every function is implemented yet. Soon!
## ⚙️ Usage

```js
//Create a spotify core client 
string dataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Eum", "Console", "Dev"));
var spotifyClient = SpotifyClient.Create(new SpotifyConfig
{
    LogPath = Path.Combine(dataDir, "Logs.txt"),
    CachePath = dataDir,
    TimeSyncMethod = TimeSyncMethod.MELODY
});
S_Log.Instance.InitializeDefaults(Path.Combine(dataDir, "Logs.txt"), null);
var authenticated = await
    spotifyClient.AuthenticateAsync(new SpotifyUserPassAuthenticator("XXX", "XXXX"));

  
//Set-up an apple core client, 
//with a login screen that is launched/powered by the ChromeDriver
var appleCore = AppleCore.Create(new DeveloperTokenConfiguration  
{  
  DefaultStorefrontId = "kr",  
  KeyId = "developer_key_id",  
  TeamId = "developer_team_id",  
  PathToFile = "authkey.p8"  
}, new ChromeAuthHandler());  
  

```

# Spotify Playback

## Setup
Creating a new instance of the SpotifyPlaybackClient, will notify the Connect-State that a new device has appeared.
Like this:

```csharp

IAudioPlayer player = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new EumVlcPlayer() : new NAudioPlayer());
ISpotifyPlaybackClient playbackClient = new SpotifyPlaybackClient(spotifyClient, player);

```

For macOS, I recommend VLC. Make sure to add a reference to LibVlc and LibVlc.Mac in your runtime project. 
For Windows, I recommended NAudio (not supported on osx because of waveout event).

It is currently not possible to initiate a playback yourself (working on it), but the app does support transfer commands.
Meaning the lib will handle a incoming play command (from another device), and transfer the state accordingly. 

If you wish to implement the audio player yourself. Check the readme in the Spotify.Playback project (working on it).

## ✏️ Examples


<br>
