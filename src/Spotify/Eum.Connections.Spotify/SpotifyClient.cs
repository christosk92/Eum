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
using Eum.Logging;
using Eum.Spotify.connectstate;
using Eum.Spotify.metadata;
using Eum.Users;
using Refit;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify;

/// <inheritdoc />
public class SpotifyClient : ISpotifyClient
{
    private SpotifyPrivateUser? _user = null;
    private readonly ISpotifyConnectionProvider _spotifyConnectionProvider;

    public SpotifyClient(ISpotifyConnectionProvider connectionProvider, 
        IBearerClient bearerClient,
        ISpotifyUsersClient usersClient, 
        SpotifyConfig config, IArtistClient artists, ITracksClient tracks, IAudioKeyManager audioKeyManager, 
        ITimeProvider timeProvider, ISpotifyConnectClient websocketState, IMercuryClient mercuryClient, IEventService eventService, IOpenPlaylistsClient openApiPlaylists, ISpClientPlaylists spClientPlaylists, IAlbumsClient albums, IMercurySearchClient search, ICacheManager? cache = null)
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
        OpenApiPlaylists = openApiPlaylists;
        SpClientPlaylists = spClientPlaylists;
        Albums = albums;
        Search = search;
        Cache = cache;
    }


    /// <summary>
    /// Initialize a new <see cref="ISpotifyClient"/> with a single config file.
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static SpotifyClient Create(SpotifyConfig config)
    {
        var holder = new SpotifyConnectionProvider(config);
        var mercuryClient = new MercuryClient(holder);
        var bearer = new MercuryBearerClient(mercuryClient);
        
        var users = BuildLoggableClient<ISpotifyUsersClient>(bearer);
        var playlists = BuildLoggableClient<IOpenPlaylistsClient>(bearer);
        var openArtists = BuildLoggableClient<IOpenArtistClient>(bearer);
        var spClientPlaylists = BuildLoggableClient<ISpClientPlaylists>(bearer);
        var artists = new ArtistsClientWrapper(new MercuryArtistClient(mercuryClient), openArtists);

        var tracks = new TracksClientWrapper(null, new MercuryTracksClient(mercuryClient));

        var audioKeyManager = new AudioKeyManager(holder);

        ICacheManager? cacheManager = default;
        if (!string.IsNullOrEmpty(config.CachePath))
        {
            cacheManager = new JournalCacheManager(config.CachePath);
        }

        var timeProvider = new TimeProvider(config, bearer);
        var albumsClientWrapper = new AlbumsCLientWrapper(new MercuryAlbumClient(mercuryClient));
        
        var websocketState = new SpotifyConnectClient(new SpotifyWebSocket(bearer), bearer);
        var search = new MercurySearchClient(mercuryClient);
        return new SpotifyClient(holder, bearer, users,  
            config, artists, tracks, audioKeyManager, 
            timeProvider, websocketState, mercuryClient,
            new EventService(mercuryClient, timeProvider), playlists,
            spClientPlaylists,
            albumsClientWrapper,
            search,
            cacheManager);
    }

    public bool IsAuthenticated => 
        _spotifyConnectionProvider.IsConnected && _spotifyConnectionProvider.GetCurrentUser() != null;

    public event EventHandler<AuthenticatedSpotifyUser>? Authenticated;
    AuthenticatedSpotifyUser? ISpotifyClient.AuthenticatedUser => _spotifyConnectionProvider.GetCurrentUser();
    public SpotifyPrivateUser? PrivateUser => _user;
    public CoreType Type => CoreType.Spotify;

    public IUser? AuthenticatedUser => !IsAuthenticated ? null : _user;

    public ISpotifyUsersClient Users { get; }
    public IBearerClient BearerClient { get; }
    public ITracksClient Tracks { get; }
    public IArtistClient Artists { get; }
    public IAlbumsClient Albums { get; }
    public IMercurySearchClient Search { get; }
    public IAudioKeyManager AudioKeyManager { get; }
    public SpotifyConfig Config { get; }
    public ICacheManager? Cache { get; }
    public ISpotifyConnectClient WebsocketState { get; }
    public ITimeProvider TimeProvider { get; }
    public IMercuryClient MercuryClient { get; }
    public IEventService EventService { get; }
    public IOpenPlaylistsClient OpenApiPlaylists { get; }
    public ISpClientPlaylists SpClientPlaylists { get; }

    public async Task<AuthenticatedSpotifyUser?> AuthenticateAsync(ISpotifyAuthentication authentication)
    {
        if (_spotifyConnectionProvider.GetCurrentUser() != null)
            return _spotifyConnectionProvider.GetCurrentUser();

        S_Log.Instance.LogInfo("1");
        await _spotifyConnectionProvider.GetConnectionAsync(authentication);
        S_Log.Instance.LogInfo("2");
        _user = await Users.GetCurrentUser();
        S_Log.Instance.LogInfo("3");
        try
        {
            await TimeProvider.Init();
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
        }

        S_Log.Instance.LogInfo("4");
        await WebsocketState.Authenticate();
        S_Log.Instance.LogInfo("5");
        Authenticated?.Invoke(this, _spotifyConnectionProvider.GetCurrentUser());
        S_Log.Instance.LogInfo("6");
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
        //TODO: ApResolver
        //gae2-spclient.spotify.com:443
        if (attribute.Any(x => x is SpClientEndpoint)) return "https://gae2-spclient.spotify.com/";

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

    /// <summary>
    /// The hexademical representation of the device id. If this is null, a new one will be generated.
    /// </summary>
    public string DeviceId { get; init; } = Utils.RandomHexString(40).ToLower();
    /// <summary>
    /// A bool indicating if the should retry a chunk if it fails. Default is true.
    /// </summary>
    public bool RetryOnChunkError { get; set; } = true;
    
    /// <summary>
    /// The path to the cache folder. If this is null, no caching will be done.
    /// </summary>
    public string? CachePath { get; init; }
    
    /// <summary>
    /// The initial volume of the player. Default is MAX_VOLUME / 2.
    /// </summary>
    public uint InitialVolume { get; init; } = 65536 / 2;
    
    /// <summary>
    /// The name of the device. Default is "Eum Desktop".
    /// </summary>
    public string DeviceName { get; init; } = "Eum Desktop";
    
    /// <summary>
    /// The type of the device. Default is "Computer".
    /// </summary>
    public DeviceType DeviceType { get; init; } = DeviceType.Computer;
    
    /// <summary>
    /// The step size of the volume. Default is VOLUME_MAX / 100.
    /// </summary>
    public int VolumeSteps { get; init; } = 64;
    
    /// <summary>
    /// Type of <see cref="TimeSyncMethod"/>. Default is <see cref="TimeSyncMethod.MELODY"/>. 
    /// </summary>
    public TimeSyncMethod TimeSyncMethod { get; init; } = TimeSyncMethod.MELODY;
    
    /// <summary>
    /// Whenever <see cref="TimeSyncMethod"/> is set to <see cref="TimeSyncMethod.Manual"/>, this is the offset that will be used.
    /// </summary>
    public long TimeManualCorrection { get; set; }
    
    /// <summary>
    /// Preferred bitrate.
    /// </summary>
    public AudioQuality AudioQuality { get; set; }
    
    
    /// <summary>
    /// Normalize the volume of the audio. Default is false.
    /// </summary>
    public bool Normalization { get; set; }
    
    /// <summary>
    /// Whenever there's nothing left to play, the player will automatically start playing a new feed from Spotify. Default is true.
    /// </summary>
    public bool AutoplayEnabled { get; set; } = true;
    /// <summary>
    /// Crossfade into the next track, time in milliseconds. Default is 10 seconds.
    /// </summary>
    public int CrossfadeDuration { get; set; } = 10000;

    /// <summary>
    /// Bool indicating if the player should preload tracks.
    /// </summary>
    public bool PreloadEnabled { get; set; } = true;
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