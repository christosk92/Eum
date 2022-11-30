using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Attributes;
using Eum.Connections.Spotify.Cache;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.Connections.Spotify.DelegatingHandlers;
using Eum.Connections.Spotify.Enums;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Websocket;
using Eum.Spotify.connectstate;
using Eum.Spotify.metadata;
using Eum.Users;
using Refit;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify;

/// <summary>
/// The main object for the Spotify connection.
/// Register this a singleton in your IoC container. Creating a new object everytime will result in a new connection.
/// <br/>
/// All functions inside this class are thread safe and can be called from any thread.
/// <br/>
/// Make sure to authenticate before using any other functions.
/// If you authenticate with different authentication credentials the connection will be reset using the new credentials,
/// including any open websockets.
/// </summary>
public class SpotifyClient : ISpotifyClient
{
    private SpotifyPrivateUser? _user = null;
    private readonly ISpotifyConnectionProvider _spotifyConnectionProvider;
    private AuthenticatedSpotifyUser _authenticatedUser;

    public SpotifyClient(ISpotifyConnectionProvider connectionProvider, 
        IBearerClient bearerClient,
        ISpotifyUsersClient usersClient, 
        SpotifyConfig config, IArtistClient artists, ITracksClient tracks, IAudioKeyManager audioKeyManager, 
        ITimeProvider timeProvider, ISpotifyConnectClient websocketState, IMercuryClient mercuryClient, IEventService eventService, ICacheManager? cache = null)
    {
        BearerClient = bearerClient;
        Users = usersClient;
        _spotifyConnectionProvider = connectionProvider;
        Config = config;
        Artists = artists;
        Tracks = tracks;
        AudioKeyManager = audioKeyManager;
        TimeProvider = timeProvider;
        WebsocketState = websocketState;
        MercuryClient = mercuryClient;
        EventService = eventService;
        Cache = cache;
    }


    public static SpotifyClient Create(SpotifyConfig config)
    {
        var holder = new SpotifyConnectionProvider(config);
        var mercuryClient = new MercuryClient(holder);
        var bearer = new MercuryBearerClient(mercuryClient);
        
        var users = BuildLoggableClient<ISpotifyUsersClient>(bearer);
        var openArtists = BuildLoggableClient<IOpenArtistClient>(bearer);
        var artists = new ArtistsClientWrapper(new MercuryArtistClient(mercuryClient), openArtists);

        var tracks = new TracksClientWrapper(null, new MercuryTracksClient(mercuryClient));

        var audioKeyManager = new AudioKeyManager(holder);

        ICacheManager? cacheManager = default;
        if (!string.IsNullOrEmpty(config.CachePath))
        {
            cacheManager = new JournalCacheManager(config.CachePath);
        }

        var timeProvider = new TimeProvider(config, bearer);
        
        
        var websocketState = new SpotifyConnectClient(new SpotifyWebSocket(bearer), bearer);
        return new SpotifyClient(holder, bearer, users,  
            config, artists, tracks, audioKeyManager, 
            timeProvider, websocketState, mercuryClient,
            new EventService(mercuryClient, timeProvider),
            cacheManager);
    }

    public bool IsAuthenticated => 
        _spotifyConnectionProvider.IsConnected && _spotifyConnectionProvider.GetCurrentUser() != null;

    AuthenticatedSpotifyUser ISpotifyClient.AuthenticatedUser => _spotifyConnectionProvider.GetCurrentUser() ?? throw new InvalidOperationException();
    public CoreType Type => CoreType.Spotify;

    public IUser? AuthenticatedUser => !IsAuthenticated ? null : _user;

    public ISpotifyUsersClient Users { get; }
    public IBearerClient BearerClient { get; }
    public ITracksClient Tracks { get; }
    public IArtistClient Artists { get; }
    public IAudioKeyManager AudioKeyManager { get; }
    public SpotifyConfig Config { get; }
    public ICacheManager? Cache { get; }
    public ISpotifyConnectClient WebsocketState { get; }
    public ITimeProvider TimeProvider { get; }
    public IMercuryClient MercuryClient { get; }
    public IEventService EventService { get; }

    public async Task<AuthenticatedSpotifyUser?> AuthenticateAsync(ISpotifyAuthentication authentication)
    {
        if (_spotifyConnectionProvider.GetCurrentUser() != null)
            return _spotifyConnectionProvider.GetCurrentUser();

        await _spotifyConnectionProvider.GetConnectionAsync(authentication);

        _user = await Users.GetCurrentUser();
        
        await TimeProvider.Init();
        await WebsocketState.Authenticate();
        
        return _spotifyConnectionProvider.GetCurrentUser();
    }

    public void Dispose()
    {
        _spotifyConnectionProvider.Dispose();
    }
    
    
    private static T BuildLoggableClient<T>(IBearerClient bearerClient)
    {
        var type = typeof(T);
        var baseUrl = ResolveBaseUrlFromAttribute(type);

        var handler = new LoggingHandler(new HttpClientHandler(), bearerClient);

        var client =
            new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };

        var refitSettings = new RefitSettings(JsonConverters.DefaultOptions.RefitSettings);
        var refitClient = RestService.For<T>(client, refitSettings);

        return refitClient;
    }

    private static string ResolveBaseUrlFromAttribute(MemberInfo type)
    {
        var attribute = Attribute.GetCustomAttributes(type);

        if (attribute.FirstOrDefault(x => x is BaseUrlAttribute) is BaseUrlAttribute baseUrlAttribute)
            return baseUrlAttribute.BaseUrl;

        // if (attribute.Any(x => x is ResolvedDealerEndpoint)) return await ApResolver.GetClosestDealerAsync();
        //
        // if (attribute.Any(x => x is ResolvedSpClientEndpoint)) return await ApResolver.GetClosestSpClient();

        if (attribute.Any(x => x is OpenUrlEndpoint)) return "https://api.spotify.com/v1";

        throw new InvalidDataException("No BaseUrl or ResolvedEndpoint attribute was defined");
    }
}

public class MercuryTracksClient : IMercuryTracksClient
{
    private readonly IMercuryClient _mercuryClient;
    public MercuryTracksClient(IMercuryClient mercuryClient)
    {
        _mercuryClient = mercuryClient;
    }

    public async Task<Track> GetTrack(string hexid, CancellationToken ct = default)
    {
        var send = await _mercuryClient.SendAndReceiveResponseAsync($"hm://metadata/4/track/{hexid}", MercuryRequestType.Get,
            ct);
        if (send.StatusCode >= 200 && send.StatusCode < 300)
            return Track.Parser.ParseFrom(send.Payload.Span);

        throw new MercuryException(send.Payload, send.StatusCode);
    }
}

public class SpotifyConfig
{
    /// <summary>
    /// Keep this null if you do not want any logging
    /// </summary>
    public string? LogPath { get; init; }

    public string DeviceId { get; init; } = Utils.RandomHexString(40).ToLower();
    public bool RetryOnChunkError { get; set; } = true;
    public string? CachePath { get; init; }
    public uint InitialVolume { get; init; } = 65536 / 2;
    public string DeviceName { get; init; } = "Eum Desktop";
    public DeviceType DeviceType { get; init; } = DeviceType.Computer;
    public int VolumeSteps { get; init; } = 64;

    public TimeSyncMethod TimeSyncMethod { get; init; } = TimeSyncMethod.MELODY;
    public long TimeManualCorrection { get; set; }
    public AudioQuality AudioQuality { get; set; }
    public bool Normalization { get; set; }
    public bool AutoplayEnabled { get; set; } = true;
    public int CrossfadeDuration { get; set; } = 10000;
    public bool PreloadEnabled { get; set; }
    public bool BypassSinkVolume { get; set; }
}


public enum TimeSyncMethod  
{
    /// <summary>
    /// Measure the time between sending and receiving a request to an NTP server.
    /// The library uses time.google.com  (Recommended).
    /// </summary>
    NTP,
    /// <summary>
    /// Measure the time between sending and receiving a request to the Spotify API.
    /// </summary>
    PING,
    /// <summary>
    /// Measure the difference in time reported by the Spotify API and the local time.
    /// </summary>
    MELODY,
    /// <summary>
    /// Manually set the time correction.
    /// </summary>
    MANUAL
}