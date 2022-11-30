using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Websocket;
using Eum.Logging;
using Eum.Spotify.connectstate;
using Flurl;
using Flurl.Http;
using Google.Protobuf;
using Nito.AsyncEx;
using Protobuf.Text;

namespace Eum.Connections.Spotify.Playback.States;

public class DeviceStateHandler : IMessageListener, IRequestListener
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly DeviceInfo _deviceInfo;

    private readonly List<IDeviceStateHandlerListener> listeners =
        new List<IDeviceStateHandlerListener>();

    private readonly PutStateRequest _putState;

    private string? ConnectionId => _spotifyClient.WebsocketState.ConnectionId;
    private volatile bool _closing = false;

    public DeviceStateHandler(ISpotifyClient spotifyClient, bool hasSink)
    {
        _spotifyClient = spotifyClient;

        _deviceInfo = InitializeDeviceInfo(spotifyClient, hasSink);
        _putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = _deviceInfo
            }
        };

        spotifyClient.WebsocketState.AddMessageListener(this,
            "hm://pusher/v1/connections/",
            "hm://connect-state/v1/connect/volume", "hm://connect-state/v1/cluster");
        spotifyClient.WebsocketState.AddRequestListener(this,
            "hm://connect-state/v1/");

        if (!spotifyClient.WebsocketState.WaitingForConnectionId)
        {
            Task.Run(async () => await NotifyReady());
        }

        spotifyClient.WebsocketState.ConnectionIdReceived += (sender, s) =>
        {
            Task.Run(async () => await NotifyReady());
        };
        /*
        session.dealer().addMessageListener(this, "hm://pusher/v1/connections/", "hm://connect-state/v1/connect/volume", "hm://connect-state/v1/cluster");
        session.dealer().addRequestListener(this, "hm://connect-state/v1/");*/
    }

    public async Task NotifyReady()
    {
        await _waitForListners.WaitAsync();
        lock (listeners)
        {
            foreach (var listener in listeners)
            {
                _ = Task.Run(async () => await listener.Ready());
            }
        }
    }

    private AsyncManualResetEvent _waitForListners = new AsyncManualResetEvent();
    public uint Volume => _deviceInfo.Volume;

    private static DeviceInfo
        InitializeDeviceInfo(ISpotifyClient spotifyClient, bool hasSink)
    {
        return new DeviceInfo
        {
            CanPlay = hasSink,
            Volume = spotifyClient.Config.InitialVolume,
            Name = spotifyClient.Config.DeviceName,
            DeviceId = spotifyClient.Config.DeviceId,
            DeviceType = spotifyClient.Config.DeviceType,
            DeviceSoftwareVersion = "1.0.0",
            SpircVersion = "3.2.6",
            Capabilities = hasSink ? new Capabilities
            {
                CanBePlayer = hasSink,
                GaiaEqConnectId = true,
                SupportsLogout = true,
                IsObservable = true,
                CommandAcks = true,
                SupportsRename = false,
                SupportsPlaylistV2 = true,
                IsControllable = hasSink,
                SupportsTransferCommand = hasSink,
                SupportsCommandRequest = hasSink,
                VolumeSteps = spotifyClient.Config.VolumeSteps,
                SupportsGzipPushes = true, 
                NeedsFullPlayerState = true,
                SupportedTypes =
                {
                    "audio/episode",
                    "audio/track"
                }
            } : null
        };
    }

    public void AddListener(IDeviceStateHandlerListener toAdd)
    {
        lock (listeners)
        {
            listeners.Add(toAdd);
        }

        _waitForListners.Set();
    }

    public void RemoveListener(IDeviceStateHandlerListener toAdd)
    {
        lock (listeners)
        {
            listeners.Remove(toAdd);
        }

        _waitForListners.Reset();
    }

    public async Task UpdateState(PutStateReason newDevice, int? playerTime, PlayerState state)
    {
        //https://open.spotify.com/track/5xrtzzzikpG3BLbo4q1Yul?si=1e6f84232f8f4810
        long timestamp = _spotifyClient.TimeProvider.CurrentTimeMillis();
        if (playerTime == null)
            _putState.HasBeenPlayingForMs = 0L;
        else
            _putState.HasBeenPlayingForMs = (ulong) (Math.Min((long) playerTime,
                (timestamp - (long) _putState.StartedPlayingAt)));

        _putState.PutStateReason = newDevice;
        _putState.ClientSideTimestamp = (ulong) timestamp;
        _putState.Device.DeviceInfo = _deviceInfo;
        _putState.Device.PlayerState = state;

        try
        {
            //TODO: ApResolver
            //gae2-spclient.spotify.com:443
            using var bytArrayContent = new ByteArrayContent(_putState.ToByteArray());
            await using var clusterResponse = await "https://gae2-spclient.spotify.com"
                .AppendPathSegments("connect-state", "v1", "devices", _spotifyClient.Config.DeviceId)
                .WithOAuthBearerToken((await _spotifyClient.BearerClient.GetBearerTokenAsync()))
                .WithHeader("X-Spotify-Connection-Id", ConnectionId)
                .WithHeader("Content-Type", "application/x-protobuf")
                .PutAsync(bytArrayContent)
                .ReceiveStream();
            S_Log.Instance.LogInfo(
                $"Put state. ts: {_putState.ClientSideTimestamp}, connId: {ConnectionId}, reason: {_putState.PutStateReason}, request: {_putState}");

            var cluster = Cluster.Parser.ParseFrom(clusterResponse);
            LatestCluster = cluster;
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError("Failed to update state.", x);
        }
    }

    public Cluster LatestCluster { get; private set; }

    public void SetIsActive(bool b)
    {
        if (b)
        {
            if (!_putState.IsActive)
            {
                var now = _spotifyClient.TimeProvider.CurrentTimeMillis();
                _putState.IsActive = true;
                _putState.StartedPlayingAt = (ulong) now;
                S_Log.Instance.LogInfo($"Device is now active. Ts: {now}");
            }
        }
        else
        {
            _putState.IsActive = false;
            _putState.StartedPlayingAt = 0L;
        }
    }

    public void OnMessage(string uri, Dictionary<string, string> headers, byte[] decodedPayload)
    {
        if (uri == "hm://connect-state/v1/connect/volume")
        {
            var cmd = SetVolumeCommand.Parser.ParseFrom(decodedPayload);
            SetVolume(cmd.Volume);
        }
        else if (uri == "hm://connect-state/v1/cluster")
        {
            var update = ClusterUpdate.Parser.ParseFrom(decodedPayload);

            var now = _spotifyClient.TimeProvider.CurrentTimeMillis();
            S_Log.Instance.LogInfo($"Received cluster update at {now}");

            LatestCluster = update.Cluster;
            long ts = update.Cluster.Timestamp - 3000; // Workaround
            if (!_spotifyClient.Config.DeviceId.Equals(update.Cluster.ActiveDeviceId) && IsActive &&
                now > StartedPlayingAt
                && ts > StartedPlayingAt)
            {
                lock (listeners)
                {
                    foreach (var listener in listeners)
                    {
                        listener.NotActive();
                    }
                }
            }
        }
    }

    public long StartedPlayingAt => (long) _putState.StartedPlayingAt;

    public bool IsActive => _putState.IsActive;
    public string? LastCommandSentByDeviceId => _putState.LastCommandSentByDeviceId;

    public void SetVolume(int val)
    {
        _deviceInfo.Volume = (uint) val;
        lock (listeners)
        {
            foreach (var listener in listeners)
            {
                listener.VolumeChanged();
            }
        }

        S_Log.Instance.LogInfo($"Update volume. volume: {val}/{SpotifyPlaybackClient.VOLUME_MAX}");
    }

    public async ValueTask<RequestResult> OnRequest(string mid, int pid, string? sender, JsonElement command)
    {
        _putState.LastCommandSentByDeviceId = sender;


        var endpoint = command.GetProperty("endpoint").GetString() switch
        {
            "play" => CommandEndpoint.Play,
            "pause" => CommandEndpoint.Pause,
            "resume" => CommandEndpoint.Resume,
            "seek_to" => CommandEndpoint.SeekTo,
            "skip_next" => CommandEndpoint.SkipNext,
            "skip_prev" => CommandEndpoint.SkipPrev,
            "set_shuffling_context" => CommandEndpoint.SetShufflingContext,
            "set_repeating_context" => CommandEndpoint.SetRepeatingContext,
            "set_repeating_track" => CommandEndpoint.SetRepeatingTrack,
            "update_context" => CommandEndpoint.UpdateContext,
            "set_queue" => CommandEndpoint.SetQueue,
            "add_to_queue" => CommandEndpoint.AddToQueue,
            "transfer" => CommandEndpoint.Transfer,
            _ => CommandEndpoint.Error
        };
    
        await NotifyCommand(endpoint, new CommandBody(command));
        return RequestResult.SUCCESS;
    }

    private async Task NotifyCommand(CommandEndpoint endpoint, CommandBody data)
    {
        if (!listeners.Any())
        {
            S_Log.Instance.LogWarning($"Cannot dispatch command because there are no listeners. command: {endpoint}");
            return;
        }

        foreach (var deviceStateHandlerListener in listeners)
        {
            try
            {
                await deviceStateHandlerListener.Command(endpoint, data);
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
            }
        }
    }
}