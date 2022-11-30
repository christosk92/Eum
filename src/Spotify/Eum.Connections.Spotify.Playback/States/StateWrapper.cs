using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.Audio;
using Eum.Connections.Spotify.Playback.Contexts;
using Eum.Connections.Spotify.Websocket;
using Eum.Enums;
using Eum.Logging;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.metadata;
using Eum.Spotify.transfer;
using Google.Protobuf.Collections;
using Org.BouncyCastle.Utilities.Encoders;
using ContextPlayerOptions = Eum.Spotify.connectstate.ContextPlayerOptions;
using PlayOrigin = Eum.Spotify.connectstate.PlayOrigin;
using Restrictions = Eum.Spotify.connectstate.Restrictions;

namespace Eum.Connections.Spotify.Playback.States;

public class StateWrapper : IMessageListener, IDeviceStateHandlerListener
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly ISpotifyPlaybackClient _player;
    private DeviceStateHandler _device;

    public PlayerState State { get; }

    private TracksKeeper? _tracksKeeper;

    public StateWrapper(ISpotifyClient spotifyClient,
        ISpotifyPlaybackClient spotifyPlaybackClient,
        bool hasSink)
    {
        _spotifyClient = spotifyClient;
        _player = spotifyPlaybackClient;

        _device = new DeviceStateHandler(spotifyClient, hasSink);

        State = InitState(new PlayerState());

        _device.AddListener(this);

        _spotifyClient.WebsocketState.AddMessageListener(this,
            "spotify:user:attributes:update",
            "hm://playlist/",
            "hm://collection/collection/"
            + spotifyClient.AuthenticatedUser.Username + "/json");
    }

    public AbsSpotifyContext? Context { get; private set; }

    private static PlayerState InitState(PlayerState playerState)
    {
        playerState.PlaybackSpeed = 1.0;
        playerState.SessionId = string.Empty;
        playerState.PlaybackId = string.Empty;
        playerState.Suppressions = new Suppressions();
        playerState.ContextRestrictions = new Restrictions();
        playerState.Options = new ContextPlayerOptions
        {
            RepeatingContext = false,
            RepeatingTrack = false,
            ShufflingContext = false
        };
        playerState.PositionAsOfTimestamp = 0;
        playerState.Position = 0;
        playerState.IsPlaying = false;
        return playerState;
    }

    public long GetPosition()
    {
        int diff = (int) (_spotifyClient.TimeProvider.CurrentTimeMillis() - State.Timestamp);
        return Math.Max(0, (int) (State.PositionAsOfTimestamp + diff));
    }

    public string? ContextUri { get; }
    public SpotifyId? CurrentPlayable => _tracksKeeper == null ? null : new SpotifyId(State.Track.Uri);

    public void AddListener(IDeviceStateHandlerListener listener)
    {
        Device.AddListener(listener);
    }

    public DeviceStateHandler Device => _device;
    public uint Volume => Device.Volume;

    public async ValueTask Ready()
    {
        State.IsSystemInitiated = true;
        await _device.UpdateState(PutStateReason.NewDevice, null, State);
        S_Log.Instance.LogInfo("Notified new device (us)!");
    }

    public ValueTask Command(CommandEndpoint endpoint, CommandBody data)
    {
        //not interested
        return ValueTask.CompletedTask;
    }

    public async ValueTask VolumeChanged()
    {
        await _device.UpdateState(PutStateReason.VolumeChanged, _player.Time, State);
    }

    public async ValueTask NotActive()
    {
        State.Timestamp = 0L;
        State.ContextUri = string.Empty;
        State.ContextUrl = string.Empty;
        State.ContextRestrictions = null;
        State.PlayOrigin = null;
        State.Index = null;
        State.Track = null;
        State.PlaybackId = string.Empty;
        State.PlaybackSpeed = 0.0;
        State.PositionAsOfTimestamp = 0L;
        State.IsPlaying = false;
        State.Duration = 0L;
        State.IsPaused = false;
        State.IsBuffering = false;
        State.IsSystemInitiated = false;
        State.Options = null;
        State.Restrictions = null;
        State.Suppressions = null;
        State.PrevTracks.Clear();
        State.NextTracks.Clear();
        State.ContextMetadata.Clear();
        State.PageMetadata.Clear();
        State.SessionId = string.Empty;
        State.QueueRevision = string.Empty;
        State.Position = 0L;
        State.Reverse.Clear();
        State.Future.Clear();
        State.PlaybackQuality = null;

        InitState(State);
        _device.SetIsActive(false);
        await _device.UpdateState(PutStateReason.BecameInactive, _player.Time, State);
        S_Log.Instance.LogInfo("Notified inactivity!");
    }

    public void SetState(bool playing, bool paused, bool buffering)
    {
        if (paused && !playing) throw new ArgumentException();
        else if (buffering && !playing) throw new ArgumentException();

        var wasPaused = State.IsPlaying && State.IsPaused;
        State.IsPlaying = playing;
        State.IsPaused = paused;
        State.IsBuffering = buffering;

        if (wasPaused && !paused) // Assume the position was set immediately before pausing
            SetPosition(State.PositionAsOfTimestamp);
    }

    private void UpdateRestrictions()
    {
        if (Context == null) return;

        if (_tracksKeeper.IsPlayingFirst && !IsRepeatingContext)
            Context.Restrictions.Disallow(RestrictionsManager.AllowedAction.SKIP_PREV,
                RestrictionsManager.REASON_NO_PREV_TRACK);
        else
            Context.Restrictions.Allow(RestrictionsManager.AllowedAction.SKIP_PREV);

        if (_tracksKeeper.IsPlayingLast && !IsRepeatingContext)
            Context.Restrictions.Disallow(RestrictionsManager.AllowedAction.SKIP_NEXT,
                RestrictionsManager.REASON_NO_NEXT_TRACK);
        else
            Context.Restrictions.Allow(RestrictionsManager.AllowedAction.SKIP_NEXT);

        State.Restrictions = Context.Restrictions.ToProto();
        State.ContextRestrictions = Context.Restrictions.ToProto();
        // state.setRestrictions(_context.Restrictions.toProto());
        // state.setContextRestrictions(context.restrictions.toProto());
    }

    public bool IsRepeatingContext => State.Options.RepeatingContext;

    public async Task Updated()
    {
        UpdateRestrictions();
        await _device.UpdateState(PutStateReason.PlayerStateChanged, _player.Time,
            State);
    }

    public void OnMessage(string uri, Dictionary<string, string> headers, byte[] decodedPayload)
    {
        //TODO: update collections
    }

    public Cluster LatestCluster => _device.LatestCluster;

    public void SetPosition(long pos)
    {
        State.Timestamp = _spotifyClient.TimeProvider.CurrentTimeMillis();
        State.PositionAsOfTimestamp = Math.Max(0,pos);
        State.Position = 0L;
    }

    public static string GeneratePlaybackId()
    {
        byte[] bytes = new byte[16];
        new Random().NextBytes(bytes);
        bytes[0] = 1;
        return bytes.BytesToHex().ToLower();
    }

    public async Task<string> LoadContext(string uri)
    {
        State.PlayOrigin = new PlayOrigin();
        State.Options = new ContextPlayerOptions();

        var sessionId = SetContext(uri);
        await _tracksKeeper.InitializeStart();
        SetPosition(0);

        await LoadTransforming();
        return sessionId;
    }

    private async Task LoadTransforming()
    {
        var url = State.ContextMetadata.GetValueOrDefault("transforming.url", null);
        if (url == null) return;
        var shuffle = false;
        if (State.ContextMetadata.TryGetValue("transforming.shuffle", out var shuffle_property))
            shuffle = bool.Parse(shuffle_property);
        
        var willRequest = !State.Track.Metadata.ContainsKey("audio.fwdbtn.fade_overlap"); // I don't see another way to do this
        S_Log.Instance.LogInfo($"Context has transforming! url: {url}, shuffle: {shuffle}, willRequest: {willRequest}");
        if (!willRequest) return;
        
        var obj = ProtoUtils.CraftContextStateCombo(State, _tracksKeeper.Tracks);
        //perform a POST http call to url
        Debugger.Break();

    }

    private string SetContext(string uri)
    {
        Context = AbsSpotifyContext.From(uri);
        State.ContextUri = uri;

        if (!Context.IsFinite)
        {
            SetRepeatingContext(false);
            SetShufflingContext(false);
        }

        State.ContextUrl = string.Empty;
        State.Restrictions = new Restrictions();
        State.ContextRestrictions = new Restrictions();
        State.ContextMetadata.Clear();
        State.ContextMetadata.Clear();


        Pages = PagesLoader.From(_spotifyClient, uri);
        _tracksKeeper = new TracksKeeper(this, Pages);


        Device.SetIsActive(true);

        return RenewSessionId();
    }

    public PagesLoader Pages { get; set; }
    public ISpotifyClient SpotifyClient => _spotifyClient;

    public int ContextSize
    {
        get
        {
            if (State.ContextMetadata.TryGetValue("track_count", out var trackCount_str))
            {
                return int.Parse(trackCount_str);
            }
            else if (_tracksKeeper != null)
            {
                return _tracksKeeper.Tracks.Count();
            }
            else return 0;
        }
    }

    public bool IsPaused => State.IsPlaying && State.IsPaused;
    internal TracksKeeper Tracks => _tracksKeeper;

    void SetRepeatingContext(bool value)
    {
        if (Context == null) return;

        State.Options.RepeatingContext =
            value && Context.Restrictions.Can(RestrictionsManager.AllowedAction.REPEAT_CONTEXT);
    }

    public void SetShufflingContext(bool value)
    {
        if (Context == null || _tracksKeeper == null) return;

        var old = State.Options.ShufflingContext;
        State.Options.ShufflingContext = value && Context.Restrictions.Can(RestrictionsManager.AllowedAction.SHUFFLE);

        //if (old != State.Options.ShufflingContext) _tracksKeeper.ToggleShuffle(isShufflingContext());
    }

    private string RenewSessionId()
    {
        var sessionId = GenerateSessionId();
        State.SessionId = sessionId;
        return sessionId;
    }

    private static string GenerateSessionId()
    {
        byte[] bytes = new byte[16];
        new Random().NextBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("-", "");
    }

    public void SetIsBuffering(bool b)
    {
        SetState(true, State.IsPaused, b);
    }

    public void EnrichWithMetadata(MetadataWrapper metadataWrapper)
    {
        if (metadataWrapper?.track != null)
        {
            EnrichWithMetadata(metadataWrapper.track);
        }
    }

    private void EnrichWithMetadata(Track track)
    {
        if (!ProtoUtils.IsTrack(State.Track, track))
        {
            S_Log.Instance.LogInfo("Failed updating metadata: tracks do not match. Current: " + State.Track +
                                   ", new: " + track);
            return;
        }

        if (track.HasDuration) _tracksKeeper.UpdateTrackDuration(track.Duration);
        var builder = State.Track;

        if (track.HasPopularity) builder.Metadata["popularity"] = track.Popularity.ToString();
        if (track.HasExplicit) builder.Metadata["explicit"] = track.Explicit.ToString();
        if (track.HasHasLyrics) builder.Metadata["has_lyrics"] = track.HasLyrics.ToString();
        if (track.HasName) builder.Metadata["title"] = track.Name;
        if (track.HasDiscNumber) builder.Metadata["album_disc_number"] = track.DiscNumber.ToString();

        for (int i = 0; i < track.Artist.Count; i++)
        {
            var artist = track.Artist[i];
            if (artist.HasName) builder.Metadata["artist_name" + (i == 0 ? "" : (":" + 1))] = artist.Name;
            if (artist.HasGid)
                builder.Metadata["artist_uri" + (i == 0 ? "" : (":" + 1))] =
                    new SpotifyId(artist.Gid, EntityType.Artist).Uri;
        }

        if (track.Album != null)
        {
            var album = track.Album;

            if (album.Disc.Count > 0)
            {
                builder.Metadata["album_disc_count"] = album.Disc.Count.ToString();
                builder.Metadata["album_track_count"] = album.Disc.Sum(a => a.Track.Count).ToString();
            }

            if (album.HasName) builder.Metadata["album_title"] = album.Name;
            if (album.HasGid) builder.Metadata["album_uri"] = new SpotifyId(album.Gid, EntityType.Album).Uri;

            for (int i = 0; i < album.Artist.Count; i++)
            {
                var artist = album.Artist[i];
                if (artist.HasName) builder.Metadata["album_artist_name" + (i == 0 ? "" : (":" + 1))] = artist.Name;
                if (artist.HasGid)
                    builder.Metadata["album_artist_uri" + (i == 0 ? "" : (":" + 1))] =
                        new SpotifyId(artist.Gid, EntityType.Artist).Uri;
            }

            if (track.HasDiscNumber)
            {
                foreach (var disc in album.Disc)
                {
                    if (disc.Number != track.DiscNumber) continue;

                    for (int i = 0; i < disc.Track.Count; i++)
                    {
                        if (disc.Track[i].Gid.Equals(track.Gid))
                        {
                            builder.Metadata["album_track_number"] = (i + 1).ToString();
                            break;
                        }
                    }
                }
            }

            if (album.CoverGroup != null)
            {
                ImageId.PutAsMetadata(builder, album.CoverGroup);
            }
        }

        ProtoUtils.PutFilesAsMetadata(builder, track.File);

        State.Track = builder;
    }

    public async Task<string> Transfer(TransferState cmd)
    {
        var ps = cmd.CurrentSession;

        State.PlayOrigin = ProtoUtils.ConvertPlayOrigin(ps.PlayOrigin);
        State.Options = ProtoUtils.ConverterPlayerOptions(cmd.Options);
        var sessionId = SetContext(ps.Context);

        var pb = cmd.Playback;
        try
        {
            await _tracksKeeper.InitializeFrom(tracks =>
            {
                for (int i = 0; i < tracks.Count(); i++)
                {
                    var track = tracks[i];
                    if ((track.HasUid && ps.CurrentUid.Equals(track.Uid)) ||
                        ProtoUtils.trackEquals(track, pb.CurrentTrack))
                        return i;
                }

                return -1;
            }, pb.CurrentTrack, cmd.Queue);
        }
        catch (Exception ex)
        {
            S_Log.Instance.LogWarning($"Failed initializing tracks, falling back to start. uid: {ps.CurrentUid}");
            await _tracksKeeper.InitializeStart();
        }

        State.PositionAsOfTimestamp = pb.PositionAsOfTimestamp;
        if (pb.IsPaused)
            State.Timestamp = _spotifyClient.TimeProvider.CurrentTimeMillis();
        else
            State.Timestamp = pb.Timestamp;

        await LoadTransforming();

        return sessionId;
    }

    private string SetContext(global::Eum.Spotify.context.Context ctx)
    {
        var uri = ctx.Uri;
        Context = AbsSpotifyContext.From(uri);
        State.ContextUri = uri;

        if (!Context.IsFinite)
        {
            SetRepeatingContext(false);
            SetShufflingContext(false);
        }

        if (ctx.HasUrl) State.ContextUrl = ctx.Url;
        else State.ContextUrl = string.Empty;

        State.ContextMetadata.Clear();
        foreach (var (k,v) in ctx.Metadata)
        {
            State.ContextMetadata[k] = v;
        }
        Pages = PagesLoader.From(_spotifyClient, ctx);
        _tracksKeeper = new TracksKeeper(this, Pages);

        _device.SetIsActive(true);

        return RenewSessionId();
    }

    public async ValueTask<NextPlayableType> NextPlayable(bool autoplayEnabled)
    {
        if (_tracksKeeper == null) return NextPlayableType.MISSING_TRACKS;

        try
        {
            return await _tracksKeeper.NextPlayable(autoplayEnabled);
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError("Failed fetching next playable.", x);
            return NextPlayableType.MISSING_TRACKS;
        }
    }

    public async ValueTask<SpotifyId?> NextPlayableDoNotSet()
    {
        try
        {
            var id = await _tracksKeeper.NextPlayableDoNotSet();
            return id?.Id;
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError("Failed fetching next playable.", x);
            return null;
        }
    }

    public async Task<string> Load(JsonElement dataObject)
    {
        var playOrigin = ProtoUtils.JsonToPlayOrigin(PlayCommandHelper.GetPlayOrigin(ref dataObject));

        var options_json = PlayCommandHelper.GetPlayerOptions(ref dataObject);
        var options = ProtoUtils.JsonToPlayerOptions(ref options_json, State.Options);

        State.PlayOrigin = playOrigin;
        State.Options = options;

        var context_json = PlayCommandHelper.GetContext(ref dataObject);
        var sessionId = SetContext(ProtoUtils.JsonToContext(ref context_json));

        var trackUid = PlayCommandHelper.GetTrackUid(ref dataObject);
        var trackUri = PlayCommandHelper.GetSkipToTrackUri(ref dataObject);
        var trackIndex = PlayCommandHelper.GetSkipToIndex(ref dataObject);

        try
        {
            if (!string.IsNullOrEmpty(trackUri))
            {
                await _tracksKeeper.InitializeFrom(tracks =>
                {
                    var index = tracks.FindIndex(t => t.HasUri && t.Uri.Equals(trackUri));
                    return index;
                }, null, null);
            }
            else if (!string.IsNullOrEmpty(trackUid))
            {
                await _tracksKeeper.InitializeFrom(tracks =>
                {
                    var index = tracks.FindIndex(t => t.HasUid && t.Uid.Equals(trackUid));
                    return index;
                }, null, null);
            }
            else if (trackIndex != null)
            {
                await _tracksKeeper.InitializeFrom(tracks =>
                {
                    if (trackIndex.Value < tracks.Count) return trackIndex.Value;
                    return -1;
                }, null, null);
            }
            else
            {
                await _tracksKeeper.InitializeStart();
            }
        }
        catch (Exception x)
        {
            S_Log.Instance.LogWarning($"Failed to load track, falling back to start. Exception: {x.ToString()}");
            await _tracksKeeper.InitializeStart();
        }

        var seekTo = PlayCommandHelper.GetSeekTo(ref dataObject);
        if (seekTo.HasValue)
        {
            SetPosition(seekTo.Value);
        }
        else
        {
            SetPosition(0);
        }

        return sessionId;
    }
}

public enum NextPlayableType
{
    MISSING_TRACKS,
    OK_REPEAT,
    OK_PLAY,
    AUTOPLAY,
    OK_PAUSE
}

public class PagesLoader
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly List<ContextPage> _pages;
    private int currentPage = -1;

    private PagesLoader(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
        _pages = new List<ContextPage>();
    }

    public static PagesLoader From(ISpotifyClient spotifyClient, string uri)
    {
        var loader = new PagesLoader(spotifyClient)
        {
            ResolveUrl = uri
        };
        return loader;
    }

    public string ResolveUrl { get; set; }
    public Task<List<ContextTrack>> CurrentPage => GetPage(currentPage);

    public async Task<bool> NextPage()
    {
        try
        {
            await GetPage(currentPage + 1);
            currentPage++;
            return true;
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
            return false;
        }
    }

    private async Task<List<ContextTrack>> GetPage(int i)
    {
        if (i == -1) throw new ArgumentException();

        if (i == 0 && !_pages.Any() && ResolveUrl != null)
        {
            try
            {
                //TODO: Handle dynamic contexts
                var resolveContext = await _spotifyClient
                    .MercuryClient.SendAndReceiveResponseAsync($"hm://context-resolve/v1/{ResolveUrl}");
                using var jsonDocument = JsonDocument.Parse(resolveContext.Payload);
                var root = jsonDocument.RootElement;
                var pages = root.GetProperty("pages");
                foreach (var page in pages.EnumerateArray())
                {
                    var contextPage = new ContextPage(ProtoUtils.JsonToContextPage(page));
                    _pages.Add(contextPage);
                }
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                throw;
            }
        }

        ResolveUrl = null;
        if (i < _pages.Count)
        {
            var page = _pages[i];

            var tracks = await ResolvePage(page);
            page.PageUrl = string.Empty;
            page.Tracks.Clear();
            page.Tracks.AddRange(tracks);
            _pages[i] = page;
            return tracks;
        }

        return null;
    }

    private async Task<List<ContextTrack>> ResolvePage(ContextPage page)
    {
        if (page.Tracks.Count > 0)
        {
            return page.Tracks.ToList();
        }

        Debugger.Break();
        return new List<ContextTrack>();
    }

    public static PagesLoader From(ISpotifyClient session, Context context)
    {
        List<ContextPage> pages = context.Pages.ToList();
        if (!pages.Any()) 
            return From(session, context.Uri);

        var loader = new PagesLoader(session);
        loader.PutFirstPages(pages, SpotifyId.InferUriPrefix(context.Uri));
        return loader;
    }

    private void PutFirstPages(List<ContextPage> pages, string contextUri)
    {
        if (currentPage != -1 || _pages.Any()) throw new ArgumentException();
        foreach (var page in pages)
        {
            var tracks = page.Tracks.ToList();
            SanitizeTracks(tracks, contextUri == null ? EntityType.Track : SpotifyId.InferUriType(contextUri));
            page.Tracks.Clear();
            page.Tracks.AddRange(tracks);
            _pages.Add(page);
        }
    }

    private static void SanitizeTracks(List<ContextTrack> tracks, EntityType uriPrefix)
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            var builder = tracks[i];
            if ((builder.HasUri && !string.IsNullOrEmpty(builder.Uri)) || !builder.HasGid) continue;

            builder.Uri = new SpotifyId(builder.Gid, uriPrefix).Uri;
            tracks[i] = builder;
        }
    }
}

public interface IDeviceStateHandlerListener
{
    ValueTask Ready();

    ValueTask Command(CommandEndpoint endpoint,
        CommandBody data);

    ValueTask VolumeChanged();

    ValueTask NotActive();
}

public readonly record struct CommandBody(JsonElement Object, byte[] Data, string? Value)
{
    public CommandBody(JsonElement obj) : this(obj, Array.Empty<byte>(), null)
    {
        Object = obj;

        if (obj.TryGetProperty("data", out var data_Base64))
        {
            Data = Base64.Decode(data_Base64.GetString());
        }

        if (obj.TryGetProperty("value", out var value))
        {
            Value = value.ToString();
        }
    }


    public int? ValueInt()
    {
        return Value == null ? null : int.Parse(Value);
    }

    public bool? ValueBool()
    {
        return Value == null ? null : bool.Parse(Value);
    }
}