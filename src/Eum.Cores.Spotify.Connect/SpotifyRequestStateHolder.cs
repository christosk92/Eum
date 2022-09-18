using Connectstate;
using Eum.Cores.Spotify.Contracts.Models;
using Microsoft.Extensions.Options;

namespace Eum.Cores.Spotify.Connect;

public sealed class SpotifyRequestStateHolder
{
    private readonly PlayerState _playerState;
    private readonly PutStateRequest _putStateRequest;

    public SpotifyRequestStateHolder(IOptions<SpotifyConfig> config)
    {
        _playerState = new PlayerState
        {
            PlaybackSpeed = 1.0,
            SessionId = string.Empty,
            PlaybackId = string.Empty,
            Suppressions = new Suppressions(),
            ContextRestrictions = new Restrictions(),
            Options = new ContextPlayerOptions
            {
                RepeatingTrack = false,
                ShufflingContext = false,
                RepeatingContext = false
            },
            Position = 0,
            PositionAsOfTimestamp = 0,
            IsPlaying = false,
            IsSystemInitiated = true
        };
        _putStateRequest = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = 65536,
                    Name = config.Value.DeviceName,
                    DeviceId = config.Value.DeviceId,
                    DeviceType = DeviceType.Computer,
                    DeviceSoftwareVersion = "Spotify-11.1.0",
                    SpircVersion = "3.2.6",
                    Capabilities = new Capabilities
                    {
                        CanBePlayer = true,
                        GaiaEqConnectId = true,
                        SupportsLogout = true,
                        VolumeSteps = 64,
                        IsObservable = true,
                        CommandAcks = true,
                        SupportsRename = false,
                        SupportsPlaylistV2 = true,
                        IsControllable = true,
                        SupportsCommandRequest = true,
                        SupportsTransferCommand = true,
                        SupportsGzipPushes = true,
                        NeedsFullPlayerState = false,
                        SupportedTypes =
                        {
                            "audio/episode",
                            "audio/track"
                        }
                    }
                }
            }
        };
    }

    public PlayerState PlayerState => _playerState;
    public PutStateRequest PutStateRequest => _putStateRequest;
    public void SetHasBeenPlayingForMs(ulong val) => _putStateRequest.HasBeenPlayingForMs = val;
}