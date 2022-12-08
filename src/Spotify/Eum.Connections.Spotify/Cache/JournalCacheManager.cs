using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Audio;
using Eum.Connections.Spotify.Helpers;
using Eum.Logging;
using Google.Protobuf;
using Medialoc.Shared.Helpers;
using Nito.AsyncEx;
using Org.BouncyCastle.Math;

namespace Eum.Connections.Spotify.Cache;

public class JournalCacheManager : ICacheManager
{
    private readonly string _root;
    private readonly ConcurrentDictionary<string, ICacheHandler> _handlers = new();
    private readonly CacheJournal _journal;
    public const int HEADER_TIMESTAMP = 254;
    private const int HEADER_HASH = 253;

    public JournalCacheManager(string configCachePath)
    {
        IoHelpers.EnsureDirectoryExists(configCachePath);
        _root = configCachePath;
        _journal = new CacheJournal(configCachePath);
    }

    public ICacheHandler GetHandler(string id)
    {
        if (!_handlers.TryGetValue(id, out var handler))
        {
            handler = new CacheHandler(id, GetCacheFile(_root, id), _journal);
            _handlers[id] = handler;
        }

        return handler;
    }

    private static string GetCacheFile(string parent, string hex)
    {
        var dir = hex.Substring(0, 2);
        parent = Path.Combine(parent, dir);
        IoHelpers.EnsureDirectoryExists(parent);
        return Path.Combine(parent, hex);
    }

    public ICacheHandler GetHandler(ByteString? trackId = null, ByteString? episodeId = null)
    {
        return GetHandler(episodeId != null ? episodeId.BytesToHex() : trackId!.BytesToHex());
    }

    public class CacheHandler : ICacheHandler
    {
        private readonly AsyncLock _ioLock = new AsyncLock();
        private readonly string _streamId;
        private readonly FileStream io;
        private bool updatedTimestamp = false;
        private readonly CacheJournal _journal;
        public CacheHandler(string streamId, string cacheFile,
            CacheJournal journal)
        {
            _streamId = streamId;
            _journal = journal;
            io = File.Open(cacheFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            journal.CreateIfNeeded(streamId);

        }
        
        public byte[] GetHeader(byte id)
        {
            var header = _journal.GetHeader(_streamId, id);
            return header == null ? null : header.ValueBytes;
        }

        public byte[] ReadChunk(int i)
        {
            UpdateTimestamp();
            using (_ioLock.Lock())
            {
                io.Seek((long)i * CHUNK_SIZE, SeekOrigin.Begin);

                byte[] buffer = new byte[CHUNK_SIZE];
                int read = io.Read(buffer,0, buffer.Length);
                if (read != buffer.Length)
                    throw new IOException(string.Format("Couldn't read full chunk, read: {0}, needed: {1}", read,
                        buffer.Length));

                if (i == 0)
                {
                    var header = _journal.GetHeader(_streamId, HEADER_HASH);
                    if (header != null)
                    {
                        try
                        {
                            using var md5 =
                                System.Security.Cryptography.MD5.Create();
                            byte[] hash = md5.ComputeHash(buffer);
                            if (!header.ValueBytes.SequenceEqual(hash))
                            {
                                _journal.SetChunk(_streamId, i, false);
                                throw new BadChunkHashException(_streamId, header.ValueBytes, hash);
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            S_Log.Instance.LogError(ex + " Failed initializing MD5 digest.");
                        }
                    }
                }

                return buffer;
            }
        }


        private void UpdateTimestamp() {
            if (updatedTimestamp) return;

            try {
                _journal.SetHeader(_streamId, HEADER_TIMESTAMP,
                    BigInteger.ValueOf(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000).ToByteArray());
                updatedTimestamp = true;
            } catch (Exception ex)
            {
                S_Log.Instance.LogWarning("Failed updating timestamp for " + _streamId + ex.ToString());
            }
        }
        public void SetHeader(byte id, byte[] value)
        {
            try
            {
                _journal.SetHeader(_streamId, id, value);
            }
            finally
            {
                UpdateTimestamp();
            }
        }

        public bool HasChunk(int index)
        {
            UpdateTimestamp();

            using (_ioLock.Lock())
            {
                if (io.Length < (long)(index + 1) * CHUNK_SIZE)
                    return false;
            }

            return _journal.HasChunk(_streamId, index);
        }

        public async Task ReadChunk(int index, IGeneralWritableStream cdnAudioStreamer)
        {
           await cdnAudioStreamer.WriteChunk(ReadChunk(index), index, true);
        }
        

        //TODO: global variable somewhere. its duplicated now.
        public static readonly int CHUNK_SIZE = 2*128 * 1024;

        public async Task WriteChunk(byte[] buffer, int index)
        {
            using (_ioLock.Lock())
            {
                io.Seek((long)index * CHUNK_SIZE, SeekOrigin.Begin);
                await io.WriteAsync(buffer, 0, buffer.Length);
            }

            try
            {
                _journal.SetChunk(_streamId, index, true);

                if (index == 0)
                {
                    try
                    {
                        using var md5 =
                            System.Security.Cryptography.MD5.Create();
                        byte[] hash = md5.ComputeHash(buffer);
                        _journal.SetHeader(_streamId, HEADER_HASH, hash);
                    }
                    catch (TargetInvocationException ex)
                    {
                        S_Log.Instance.LogError(ex.ToString() + "  Failed initializing MD5 digest.");
                    }
                }
            }
            finally
            {
                UpdateTimestamp();
            }
        }

        public void Dispose()
        {
            io.Dispose();
        }
    }
}

public class BadChunkHashException : Exception
{
    public BadChunkHashException(object streamId, object value, byte[] hash)
    {
        throw new NotImplementedException();
    }
}