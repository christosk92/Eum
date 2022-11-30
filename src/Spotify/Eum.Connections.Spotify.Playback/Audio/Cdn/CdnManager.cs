using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Connections.Spotify.Playback.Exceptions;
using Eum.Logging;
using Eum.Spotify.metadata;
using Eum.Spotify.storage;
using Flurl;
using Flurl.Http;
using Google.Protobuf;

namespace Eum.Connections.Spotify.Playback.Audio.Cdn;

public class CdnManager : ICdnManager
{
    private readonly ISpotifyClient _spotifyClient;
    public CdnManager(ISpotifyClient spotifyClient, AudioFile file, byte[] key, string url)
    {
        _spotifyClient = spotifyClient;
        File = file;
        Key = key;
        Url = url;
    }

    public AudioFile File { get; }
    public byte[] Key { get; }
    public string Url { get; }

    public CdnAudioStreamer StreamFile(IHaltListener? haltListener, string name)
    {
        var format = File.Format.GetFormat();
        var cdnUrl = new CdnUrl(File.FileId, new Uri(Url), this);
        var audioDecrypt = new AesAudioDecrypt(Key);
        
        var streamer = new CdnAudioStreamer(_spotifyClient, File.FileId,  cdnUrl, format, audioDecrypt, name,
            _spotifyClient.Cache,
            haltListener:haltListener);

        return streamer;
    }

    public CdnAudioStreamer StreamFile()
    {
        throw new NotImplementedException();
    }

    public async Task<Uri> GetAudioUrl(ByteString fileId)
    {
        var spClient = await "https://gae2-spclient.spotify.com"
            .AppendPathSegments("/storage-resolve/files/audio/interactive", Utils.BytesToHex(fileId))
            .WithOAuthBearerToken((await _spotifyClient.BearerClient.GetBearerTokenAsync()))
            .GetAsync();
        
        if(spClient.StatusCode != (int)HttpStatusCode.OK)
            throw new FeederException("Failed to resolve storage interactive");
        
        var storageResolve = StorageResolveResponse.Parser.ParseFrom(await spClient.ResponseMessage.Content.ReadAsByteArrayAsync());
        if (storageResolve.Result == StorageResolveResponse.Types.Result.Cdn)
        {
            var url = storageResolve.Cdnurl.First();
            S_Log.Instance.LogInfo($"Fetched CDN url for {Utils.BytesToHex(fileId)}: {url}");
            return new Uri(url);
        }

        throw new CdnException($"Could not retrieve CDN url! {{result: {storageResolve.Result}");
    }
}