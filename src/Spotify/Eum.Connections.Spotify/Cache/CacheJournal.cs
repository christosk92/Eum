using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Eum.Connections.Spotify.Helpers;
using Nito.AsyncEx;
using SpotifyTcp.Helpers;

namespace Eum.Connections.Spotify.Cache;

public class CacheJournal : IDisposable
{
    public const int MAX_CHUNKS_SIZE = 2048;
    public const int MAX_CHUNKS = MAX_CHUNKS_SIZE * 8;
    public const int MAX_HEADER_LENGTH = 1023;
    public const int MAX_ID_LENGTH = 40;
    public const int MAX_HEADERS = 8;
    public const int JOURNAL_ENTRY_SIZE = MAX_ID_LENGTH + MAX_CHUNKS_SIZE + (1 + MAX_HEADER_LENGTH) * MAX_HEADERS;
    public static readonly byte[] ZERO_ARRAY = new byte[JOURNAL_ENTRY_SIZE];

    private readonly ConcurrentDictionary<string, CacheEntry> _cacheEntries =
        new ConcurrentDictionary<string, CacheEntry>();

    private readonly FileStream _fs;

    public CacheJournal(string? configCachePath)
    {
        _fs = File.Open(Path.Combine(configCachePath, "journal.dat"), FileMode.OpenOrCreate, FileAccess.ReadWrite);

    }

    public static bool CheckId(Stream io, int first, byte[] id)
    {
        for (int i = 0;
             i < id.Length;
             i++)
        {
            int read = i == 0 ? first : io.ReadByte();
            if (read == 0)
                return i != 0;

            if (read != id[i])
                return false;
        }

        return true;
    }

    public bool HasChunk(string streamId, int index)
    {
        if (index < 0 || index > MAX_CHUNKS) throw new ArgumentException();

        var entry = Find(streamId);
        if (entry == null) throw new JournalException("Couldn't find entry on journal: " + streamId);

        using(io.Lock()) {
            return entry.HasChunk(index);
        }
    }

    public void SetChunk(string streamId, int index, bool value)
    {
        if (index < 0 || index > MAX_CHUNKS) throw new ArgumentException();

        var entry = Find(streamId);
        if (entry == null) throw new JournalException("Couldn't find entry on journal: " + streamId);

        using (io.Lock())
        {
            entry.SetChunk(index, value);
        }
    }

    private CacheEntry? Find(string id)
    {
        if (id.Length > MAX_ID_LENGTH) throw new ArgumentException();

        if (_cacheEntries.TryGetValue(id, out var entry)) return entry;

        var idBytes = Encoding.ASCII.GetBytes(id);
        using (io.Lock())
        {
            _fs.Seek(0, SeekOrigin.Begin);

            int i = 0;
            while (true)
            {
                _fs.Seek((long)i * JOURNAL_ENTRY_SIZE, SeekOrigin.Begin);

                int first = _fs.ReadByte();
                if (first == -1) // EOF
                    return null;

                if (first == 0)
                {
                    // Empty spot
                    i++;
                    continue;
                }

                if (CheckId(_fs, first, idBytes))
                {
                    entry = new CacheEntry(id, i * JOURNAL_ENTRY_SIZE, _fs);
                    _cacheEntries[id] = entry;
                    return entry;
                }

                i++;
            }
        }
    }

    private readonly AsyncLock io = new AsyncLock();

    public void Dispose()
    {
        _fs.Dispose();
    }

    public void CreateIfNeeded(string id)
    {
        if (Find(id) != null) return;

        using(io.Lock()) {
            _fs.Seek(0, SeekOrigin.Begin);

            int i = 0;
            while (true)
            {
                _fs.Seek((long)i * JOURNAL_ENTRY_SIZE, SeekOrigin.Begin);

                int first = _fs.ReadByte();
                if (first == 0 || first == -1)
                {
                    // First empty spot or EOF
                    var entry = new CacheEntry(id, i * JOURNAL_ENTRY_SIZE, _fs);
                    entry.WriteId();
                    _cacheEntries[id] = entry;
                    return;
                }

                i++;
            }
        }
    }

    public JournalHeader GetHeader(string streamId, byte id)
    {
        var entry = Find(streamId);
        if (entry == null) throw new JournalException("Couldn't find entry on journal: " + streamId);

        using (io.Lock())
        {
            return entry.GetHeader(id);
        }
    }


    public void SetHeader(string streamId, int headerId, byte[] value)
    {
        var strValue = Utils.BytesToHex(value);

        if (strValue.Length > MAX_HEADER_LENGTH) throw new ArgumentOutOfRangeException();
        else if (headerId == 0) throw new ArgumentException();

        var entry = Find(streamId);
        if (entry == null) throw new JournalException("Couldn't find entry on journal: " + streamId);

        using (io.Lock())
        {
            entry.SetHeader(headerId, strValue);
        }
    }
}

public class JournalException : Exception
{
    public JournalException(string couldnTFindEntryOnJournal)
    {
        throw new NotImplementedException();
    }
}

internal class CacheEntry : IDisposable
{
    private readonly string _id;
    private readonly int _offset;
    private readonly FileStream _fs;

    public CacheEntry(string id, int offset, FileStream fs)
    {
        _id = id;
        _offset = offset;
        _fs = fs;
    }



    public void Dispose()
    {
        _fs.Dispose();
    }

    public bool HasChunk(int index)
    {
        _fs.Seek(_offset + CacheJournal.MAX_ID_LENGTH + (index / 8), SeekOrigin.Begin);
        var shifted = (int)((uint)_fs.ReadByte() >> (index % 8));
        return (shifted & 0b00000001) == 1;
    }

    public void SetChunk(int index, bool value)
    {
        int pos = _offset + CacheJournal.MAX_ID_LENGTH + (index / 8);
        _fs.Seek(pos, SeekOrigin.Begin);
        int read = _fs.ReadByte();
        if (value) read |= (1 << (index % 8));
        else read &= ~(1 << (index % 8));
        _fs.Seek(pos, SeekOrigin.Begin);
        
        //In Java. io.Write(int b) writes the byte b to the file.
        //The byte is written to the low eight bits of the argument b.
        //The 24 high-order bits of b are ignored.
        var toWrite = 
            ((byte)((read & 0x0000FFFF)));
        _fs.WriteByte(toWrite);
    }

    public void WriteId()
    {
        _fs.Seek(_offset, SeekOrigin.Begin);
        _fs.Write(Encoding.ASCII.GetBytes(_id));
        _fs.Write(CacheJournal.ZERO_ARRAY, 0,
            CacheJournal.JOURNAL_ENTRY_SIZE - _id.Length);
    }

    private int FindHeader(int headerId)
    {
        for (int i = 0; i < CacheJournal.MAX_HEADERS; i++)
        {
            _fs.Seek(_offset + CacheJournal.MAX_ID_LENGTH +
                     CacheJournal.MAX_CHUNKS_SIZE + i * (CacheJournal.MAX_HEADER_LENGTH + 1), SeekOrigin.Begin);
            var readByte = _fs.ReadByte();
            if ((readByte & 0xFF) == headerId)
                return i;
        }

        return -1;
    }

    public JournalHeader? GetHeader(byte id)
    {
        int index = FindHeader(id);
        if (index == -1) return null;

        _fs.Seek(_offset + CacheJournal.MAX_ID_LENGTH + CacheJournal.MAX_CHUNKS_SIZE
                 + (long)index * (CacheJournal.MAX_HEADER_LENGTH + 1) + 1,
            SeekOrigin.Begin);
        byte[] read = new byte[CacheJournal.MAX_HEADER_LENGTH];
        var i = _fs.Read(read);

        return new JournalHeader(id, trimArrayToNullTerminator(read));
    }

    private static string trimArrayToNullTerminator(byte[] bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
            if (bytes[i] == 0)
                return Encoding.ASCII.GetString(bytes, 0, i);

        return Encoding.ASCII.GetString(bytes);
    }

    public void SetHeader(int id, string strValue)
    {
        int index = FindHeader(id);
        if (index == -1) {
            for (int i = 0; i < CacheJournal.MAX_HEADERS; i++) {
                _fs.Seek(_offset + CacheJournal.MAX_ID_LENGTH 
                               + CacheJournal.MAX_CHUNKS_SIZE 
                               + i * (CacheJournal.MAX_HEADER_LENGTH + 1), SeekOrigin.Begin);
                if (_fs.ReadByte() == 0) {
                    index = i;
                    break;
                }
            }

            if (index == -1) throw new ArgumentException();
        }

        _fs.Seek(_offset + CacheJournal.MAX_ID_LENGTH 
                         + CacheJournal.MAX_CHUNKS_SIZE 
                         + (long) index * (CacheJournal.MAX_HEADER_LENGTH + 1), SeekOrigin.Begin);


        //In Java. io.Write(int b) writes the byte b to the file.
        //The byte is written to the low eight bits of the argument b.
        //The 24 high-order bits of b are ignored.
        var toWrite = 
            ((byte)((id & 0x0000FFFF)));
        _fs.WriteByte(toWrite);
        _fs.Write(Encoding.ASCII.GetBytes(strValue));
    }
}


public record JournalHeader(int Id, string Value)
{
    public byte[] ValueBytes => Utils.HexToBytes(Value);

    public static JournalHeader? Find(IEnumerable<JournalHeader?> headers, byte id)
    {
        return headers.FirstOrDefault(a => a.Id == id);
    }
        
        
}