using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Exceptions;
using Eum.Logging;
using Flurl;
using Flurl.Http;
using GuerrillaNtp;
using Nito.AsyncEx;

namespace Eum.Connections.Spotify.Helpers;

public class TimeProvider : ITimeProvider
{
    private AsyncLock _offsetLock = new AsyncLock();
    private double _offset;
    private readonly SpotifyConfig _config;
    private readonly IBearerClient _bearer;

    public TimeProvider(SpotifyConfig config, IBearerClient bearer)
    {
        _config = config;
        _bearer = bearer;
    }

    public async ValueTask Init()
    {
        switch (_config.TimeSyncMethod)
        {
            case TimeSyncMethod.NTP:
                try
                {
                    await UpdateWithNtp();
                }
                catch (IOException ex)
                {
                    S_Log.Instance.LogWarning(ex + " Failed updating time!");
                }

                break;
            case TimeSyncMethod.MANUAL:
                using (_offsetLock.Lock())
                {
                    _offset = _config.TimeManualCorrection;
                }

                break;
            default:
            case TimeSyncMethod.PING:
                throw new NotImplementedException();
            case TimeSyncMethod.MELODY:
                await UpdateMelody();
                break;
        }
    }

    public long CurrentTimeMillis()
    {
        using (_offsetLock.Lock())
        {
            return DateTimeOffset.UtcNow.AddMilliseconds(_offset).ToUnixTimeMilliseconds();
        }
    }

    private async Task UpdateMelody()
    {
        using (await _offsetLock.LockAsync())
        {
            //TODO: ApResolver
            //gae2-spclient.spotify.com:443
            long drift = 0;
            try
            {
                var sw = Stopwatch.StartNew();
                using var _ = await "https://gae2-spclient.spotify.com"
                    .AppendPathSegments("melody", "v1", "time")
                    .WithOAuthBearerToken((await _bearer.GetBearerTokenAsync()))
                    .OptionsAsync();
                sw.Stop();
                drift = sw.ElapsedMilliseconds;
            }
            catch (FlurlHttpException resp)
            {
                S_Log.Instance.LogError(
                    $"Failed notifying server of time request! code: {resp.StatusCode}, msg: {resp.Message}", resp);
                return;
            }
            catch (Exception m)
            {
                S_Log.Instance.LogError("Failed notifying server of time request!", m);
            }

            try
            {
                await using var data = await "https://gae2-spclient.spotify.com"
                    .AppendPathSegments("melody", "v1", "time")
                    .WithOAuthBearerToken((await _bearer.GetBearerTokenAsync()))
                    .GetStreamAsync();

                using var jsonDocument = await JsonDocument.ParseAsync(data);
                var diff = jsonDocument.RootElement.GetProperty("timestamp").GetInt64()
                           - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                _offset = diff - drift;
                S_Log.Instance.LogInfo($"Loaded time offset from Melody: {diff}ms");
            }
            catch (FlurlHttpException resp)
            {
                S_Log.Instance.LogError(
                    $"Failed requesting time! code: {resp.StatusCode}, msg: {resp.Message}", resp);
                return;
            }
            catch (Exception m)
            {
                S_Log.Instance.LogError("Failed requesting time!", m);
            }
        }
    }

    private async Task UpdateWithNtp()
    {
        try
        {
            using (await _offsetLock.LockAsync())
            {
                //var (drft, dt) = GetNetworkTime();
                //var correction = ((DateTimeOffset.UtcNow - dt).TotalMilliseconds + drft);
                using var cts = new CancellationTokenSource();
                var corrections = new double[5];
                NtpClient client = NtpClient.Default;
                cts.CancelAfter(TimeSpan.FromSeconds(3));
                var correction = (await client.QueryAsync(cts.Token)).CorrectionOffset
                    .TotalMilliseconds;
                S_Log.Instance.LogInfo($"Loaded time offset from NTP: {correction}ms");
                _offset = correction;
            }
        }
        catch (Exception ex)
        {
            await UpdateWithNtp();
        }
    }

    public static (long drift, DateTime date) GetNetworkTime()
    {
        var drift = Stopwatch.StartNew();
        //default Windows time server
        const string ntpServer = "time.google.com";
        var ntpData = new byte[48];
        ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

        var addresses = Dns.GetHostEntry(ntpServer).AddressList;
        var ipEndPoint = new IPEndPoint(addresses[0], 123);
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        socket.Connect(ipEndPoint);
        socket.Send(ntpData);
        socket.Receive(ntpData);
        socket.Close();

        ulong intPart = (ulong) ntpData[40] << 24 | (ulong) ntpData[41] << 16 | (ulong) ntpData[42] << 8 |
                        (ulong) ntpData[43];
        ulong fractPart = (ulong) ntpData[44] << 24 | (ulong) ntpData[45] << 16 | (ulong) ntpData[46] << 8 |
                          (ulong) ntpData[47];

        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
        var dt = 
            (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long) milliseconds);
        drift.Stop();
        return (drift.ElapsedMilliseconds, dt);
    }

// stackoverflow.com/a/3294698/162671
    static uint SwapEndianness(ulong x)
    {
        return (uint) (((x & 0x000000ff) << 24) +
                       ((x & 0x0000ff00) << 8) +
                       ((x & 0x00ff0000) >> 8) +
                       ((x & 0xff000000) >> 24));
    }
}

public interface ITimeProvider
{
    ValueTask Init();
    long CurrentTimeMillis();
}