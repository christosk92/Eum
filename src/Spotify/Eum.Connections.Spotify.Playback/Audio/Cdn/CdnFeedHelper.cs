using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Spotify.metadata;
using Eum.Spotify.storage;
using Google.Protobuf;

namespace Eum.Connections.Spotify.Playback.Audio.Cdn;

public static class CdnFeedHelper
{
    private static ConcurrentDictionary<ByteString, byte[]> _audiokeysCache =
        new ConcurrentDictionary<ByteString, byte[]>();
    public static async Task<LoadedStream> LoadTrack(ISpotifyClient spotifyClient, Track track, 
        AudioFile file, 
        StorageResolveResponse resp, bool preload,
        IHaltListener? haltListener,
        CancellationToken ct = default)
    {
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        byte[] key =
            _audiokeysCache.TryGetValue(file.FileId, out var k)
                ? k
                : await spotifyClient.AudioKeyManager.GetAudioKey(track.Gid, file.FileId);
        _audiokeysCache[file.FileId] = key;
        
        var audiokeyTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

        var cdnManager = new CdnManager(spotifyClient, file, key, resp.Cdnurl.First());
        var streamer = cdnManager.StreamFile(haltListener, track.Name);
        //TODO: Normalization
        object normalizationData = null;
        if(streamer.Stream.Seek(0xa7, SeekOrigin.Begin) != 0xa7) throw new Exception("Seek failed");

        return new LoadedStream(track, streamer, normalizationData,
            new PlayableContentMetrics(file.FileId, preload, preload ? -1 : audiokeyTime), new MetadataWrapper(track, null));
    }

    public static LoadedStream LoadEpisode(ISpotifyClient spotifyClient, Episode episode, AudioFile file, StorageResolveResponse resp)
    {
        throw new NotImplementedException();
    }
}

