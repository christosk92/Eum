using Eum.Connections.Spotify.Cache;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Websocket;

namespace Eum.Connections.Spotify;

/// <summary>
/// The general client you should be using to interact with Spotify. <br/>
/// The first step is to perform authentication, which can be done by calling <see cref="AuthenticateAsync"/>.
/// If the user is not authenticated, the all clients will throw an <see cref="MissingAuthenticationException"/>.
/// </summary>
public interface ISpotifyClient : IMusicCore, IDisposable
{
    /// <summary>
    /// The authenticated user response from Spotify. Will be null if no authentication was performed.
    /// </summary>
    new AuthenticatedSpotifyUser? AuthenticatedUser { get; }
    /// <summary>
    /// WebApi: Operations related to Spotify User Profiles. 
    /// </summary>
    ISpotifyUsersClient Users { get; }
    /// <summary>
    /// Mercury: Fetch and cache powerful bearer tokens.
    /// </summary>
    IBearerClient BearerClient { get; }
    /// <summary>
    /// WebApi &amp; Mercury: Operations related to Spotify Tracks.
    /// </summary>
    ITracksClient Tracks { get; }
    /// <summary>
    /// WebApi &amp; Mercury: Operations related to Spotify Artists;
    /// </summary>
    IArtistClient Artists { get; }
    
    /// <summary>
    /// Mercury: Get AES audio decryption keys.
    /// Can be used to decrypt a track for playback.
    /// </summary>
    IAudioKeyManager AudioKeyManager { get; }
    /// <summary>
    /// The spotify config used to create this client.
    /// </summary>
    SpotifyConfig Config { get; }
    /// <summary>
    /// A .dat cache file used to store cached data such as track data and chunk data.　<br/>
    /// CAUTION: Should not be used directly.
    /// </summary>
    ICacheManager? Cache { get; }
    
    /// <summary>
    /// A holder to the websocket connection.
    /// </summary>
    ISpotifyConnectClient WebsocketState { get; }
    
    /// <summary>
    /// A time synchronizer used to synchronize the time between the client and the Spotify servers.
    /// </summary>
    ITimeProvider TimeProvider { get; }
    
    /// <summary>
    /// The holder for the main mercury/tcp connection with Spotify.
    /// </summary>
    IMercuryClient MercuryClient { get;}
    
    /// <summary>
    /// Event services. This app only sends necessary events back to Spotify,
    /// to allow for a successful play to be registered.
    /// </summary>
    IEventService EventService { get; }
    
    /// <summary>
    /// Perform authentication with Spotify. <br/> <br/>
    /// Note: If the user is already authenticated, this function will have no effect. <br/>
    /// To switch between users, create a new instance instead.
    /// Create a factory yourself to fetch clients based on authentication type.
    /// </summary>
    /// <param name="authentication">The type of authentication to use.</param>
    /// <returns>The authenticated user. Also <see cref="AuthenticatedUser"/></returns>
    Task<AuthenticatedSpotifyUser?> 
        AuthenticateAsync(ISpotifyAuthentication authentication);
}