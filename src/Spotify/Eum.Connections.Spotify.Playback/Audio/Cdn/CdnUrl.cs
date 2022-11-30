using System;
using System.IO;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Exceptions;
using Eum.Connections.Spotify.Playback.Audio.Cdn;
using Eum.Logging;
using Google.Protobuf;

public class CdnUrl
{
    private readonly ByteString? _fileId;
    private long _expiration;
    private Uri _url;
    private readonly ICdnManager _cdnManager;

    public CdnUrl(ByteString fileId, Uri url, ICdnManager cdnManager)
    {
        this._fileId = fileId;
        _cdnManager = cdnManager;
        _url = null;
        _expiration = 0;
        SetUrl(url);
    }

    public void SetUrl(Uri url)
    {
        _url = url;

        if (_fileId != null)
        {
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(url.Query);

            var tokenStr = queryDictionary["__token__"];
            if (tokenStr != null && !string.IsNullOrEmpty(tokenStr))
            {
                long? expireAt = null;
                var split = tokenStr.Split('~');
                foreach (var str in split)
                {
                    int i = str.IndexOf('=');
                    if (i == -1) continue;

                    if (str.Substring(0, i).Equals("exp"))
                    {
                        expireAt = long.Parse(str.Substring(i + 1));
                        break;
                    }
                }

                if (expireAt == null)
                {
                    _expiration = -1;
                    S_Log.Instance.LogError("Invalid __token__ in CDN url: " + url);
                    return;
                }

                _expiration = (long) expireAt * 1000;
            }
            else
            {
                var param = queryDictionary.AllKeys[0];
                int i = param.IndexOf('_');
                if (i == -1)
                {
                    _expiration = -1;
                    S_Log.Instance.LogError("Couldn't extract expiration, invalid parameter in CDN url: " + url);
                    return;
                }

                _expiration = long.Parse(param.Substring(0, i)) * 1000;
            }
        }
        else
        {
            _expiration = -1;
        }
    }

    public async ValueTask<Uri> Url()
    {
        if (_expiration == -1) return _url;

        if (_expiration <= DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds())
        {
            try
            {
                _url = await _cdnManager.GetAudioUrl(_fileId);
            }
            catch (Exception x)
            {
                switch (x)
                {
                    case MercuryException:
                    case IOException:
                        throw new CdnException(x);
                        break;
                }
            }
        }

        return _url;
    }
}

public class CdnException : Exception
{
    public CdnException(Exception exception) : base(string.Empty, exception)
    {
    }

    public CdnException(string message) : base(message)
    {
    }
}