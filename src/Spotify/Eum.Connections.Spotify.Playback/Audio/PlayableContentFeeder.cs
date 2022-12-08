using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Enums;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Playback.Audio.Cdn;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Connections.Spotify.Playback.Exceptions;
using Eum.Logging;
using Eum.Spotify.metadata;
using Eum.Spotify.storage;
using Flurl;
using Flurl.Http;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Eum.Connections.Spotify.Playback.Audio;

public class PlayableContentFeeder : IDisposable
{
    private ISpotifyClient _spotifyClient;
    private const string STORAGE_RESOLVE_INTERACTIVE = "/storage-resolve/files/audio/interactive";
    private const string STORAGE_RESOLVE_INTERACTIVE_PREFETCH = "/storage-resolve/files/audio/interactive_prefetch";

    public PlayableContentFeeder(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async Task<LoadedStream> LoadTrack(Track track, AudioQuality quality, bool preload,
        IHaltListener haltListener,
        CancellationToken cancellationToken = default)
    {
        var file = GetFileForQuality(track.File, quality);
        if (file == null)
        {
            S_Log.Instance.LogError(
                $"Couldn't find any suitable audio file, available: {string.Join(Environment.NewLine, track.File.Select(a => a.FileId.ToBase64()))}");
            throw new FeederException();
        }

        return await LoadStream(file, track, null, preload, haltListener, cancellationToken);
    }

    private static AudioFile? GetFileForQuality(RepeatedField<AudioFile> trackFile, AudioQuality quality)
    {
        var vorbisFile = trackFile.GetMatches(quality)
            .FirstOrDefault(a => a.Format.GetFormat() == SuperAudioFormat.Vorbis);

        if (vorbisFile != null) return vorbisFile;
        vorbisFile = trackFile
            .FirstOrDefault(a => a.Format.GetFormat() == SuperAudioFormat.Vorbis);
        if (vorbisFile != null)
        {
            S_Log.Instance.LogWarning($"Using {vorbisFile.Format.GetFormat()} " +
                                      $"because preferred {quality} could not be found.");
        }
        else
        {
            S_Log.Instance.LogError("Couldnt find any Vorbis File.");
            return null;
        }

        return vorbisFile;
    }

    private ConcurrentDictionary<ByteString, StorageResolveResponse> _storageResolveResponsesCache =
        new ConcurrentDictionary<ByteString, StorageResolveResponse>();
    private async Task<LoadedStream> LoadStream(AudioFile file, Track? track, Episode? episode, bool preload,
        IHaltListener haltListener,
        CancellationToken ct = default)
    {
        var resp =
            _storageResolveResponsesCache.TryGetValue(file.FileId, out var cachedFIle)
                ? cachedFIle
                : await ResolveStorageInteractive(file.FileId, preload, ct);

        _storageResolveResponsesCache[file.FileId] = resp;
        switch (resp.Result)
        {
            case StorageResolveResponse.Types.Result.Cdn:
                if (track != null) return await CdnFeedHelper.LoadTrack(_spotifyClient, track, file, resp, preload, haltListener, ct);
                return CdnFeedHelper.LoadEpisode(_spotifyClient, episode, file, resp);
            case StorageResolveResponse.Types.Result.Storage:
                Debugger.Break();
                //TODO: When is this used?
                throw new NotImplementedException();
                break;
            case StorageResolveResponse.Types.Result.Restricted:
                throw new ContentRestrictedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<StorageResolveResponse> ResolveStorageInteractive(ByteString fileFileId, bool preload, CancellationToken ct = default)
    {
        //TODO: ApResolver
        //gae2-spclient.spotify.com:443
        var spClient = await "https://gae2-spclient.spotify.com"
            .AppendPathSegments(preload ? STORAGE_RESOLVE_INTERACTIVE_PREFETCH : STORAGE_RESOLVE_INTERACTIVE,
                fileFileId.BytesToHex())
            .WithOAuthBearerToken((await _spotifyClient.BearerClient.GetBearerTokenAsync(ct)))
            .GetAsync(cancellationToken: ct);
        
        if(spClient.StatusCode != (int)HttpStatusCode.OK)
            throw new FeederException("Failed to resolve storage interactive");
        
        var storageResolve = StorageResolveResponse.Parser.ParseFrom(await spClient.ResponseMessage.Content.ReadAsByteArrayAsync());
        return storageResolve;
    }

    public Track? PickAlternativeIfNecessary(Track track)
    {
        if (track.File.Count > 0) return track;

        foreach (var alt in track.Alternative)
        {
            if (alt.File.Count > 0)
            {
                track.File.Clear();
                track.File.AddRange(alt.File);
                return track;
            }
        }

        return null;
    }

    public void Dispose()
    {
        _spotifyClient = null;
    }
}
public record PlayableContentMetrics(ByteString FileId, bool Preloaded, long AudioKeyTime);