
<h1 align="center">Eum 🎵</h1>

## 🤔 Whats is this?

A merged & unified music services library for .NET 6 and up.

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
var spotifyCore = SpotifyCore.Create("user@domain.com","secret_pwd");  
  
//Set-up an apple core client, 
//with a login screen that is launched/powered by the ChromeDriver
var appleCore = AppleCore.Create(new DeveloperTokenConfiguration  
{  
  DefaultStorefrontId = "kr",  
  KeyId = "developer_key_id",  
  TeamId = "developer_team_id",  
  PathToFile = "authkey.p8"  
}, new ChromeAuthHandler());  
  
//Merge cores
var mergedCore = CoreMerger.MergeCores(spotifyCore, appleCore);  
var coreSearchResponse = await mergedCore.SearchAsync("jokjae")

//Or call each core individually!
//TODO
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

### Requiring

**Id merging**

Eum supports in-memory caching of ``CoreId``. Which is a type that holds an ``id:str`` and ``type:coretype``.
What this allows is a certain type of id-merging, where you can look-up an artist on for example ``SpotifyCore``, using the ID obtained from ``AppleCore``, and vice-versa.


Once you have your ``IMergedCore`` object, you can start caching these objects
```js
var appleId = new CoreId  
{  
  Id = "1239707923",  
  Type = CoreType.Apple  
};  
var spotifyId = new CoreId  
{  
  Id = "7bWYN0sHvyH7yv1uefX07U",  
  Type = CoreType.Spotify  
};  
  
mergedCore.MergeIds_AndForget(appleId, spotifyId);  
  
var artist =   
 await mergedCore.GetArtist(appleId);
```

<br>
