using System;
using System.Text;
using System.Text.Json.Serialization;
using Base62;
using Eum.Connections.Spotify.Helpers;
using Eum.Enums;
using Eum.Spotify.context;
using Google.Protobuf;

namespace Eum.Connections.Spotify.Models.Users;

public readonly ref struct ID_TYPES
{
    public readonly ReadOnlySpan<char> STATION;
    public readonly ReadOnlySpan<char> TRACK;
    public readonly ReadOnlySpan<char> ARTIST;
    public readonly ReadOnlySpan<char> ALBUM;
    public readonly ReadOnlySpan<char> EPISODE;
    public readonly ReadOnlySpan<char> SHOW;
    public readonly ReadOnlySpan<char> PLAYLIST;
    public readonly ReadOnlySpan<char> COLLECTION;

    public ID_TYPES()
    {
        STATION = "station";
        TRACK = "track";
        ARTIST = "artist";
        ALBUM = "album";
        EPISODE = "episode";
        SHOW = "show";
        PLAYLIST = "playlist";
        COLLECTION = "collection";
    }
}
// public readonly struct SpotifyId : IComparable<SpotifyId>, IEquatable<SpotifyId>
// {
//     public static SpotifyId With(string id, EntityType type)
//     {
//         return new($"spotify:{type.ToString().ToLower()}:{id}");
//     }
//
//     public SpotifyId WithType(EntityType type)
//     {
//         return new(
//             $"spotify:{type.ToString().ToLower()}:{Id}");
//     }
//
//     [JsonConstructor]
//     public SpotifyId(string uri)
//     {
//         Uri = uri;
//         var i = 0;
//         var idTypes = new ID_TYPES();
//         foreach (ReadOnlySpan<char> line in uri.Split(':'))
//         {
//             switch (i)
//             {
//                 case 0:
//                     break;
//                 case 1:
//                 {
//                     Type = line switch
//                     {
//                         _ when line.SequenceEqual(idTypes.STATION) => EntityType.Station,
//                         _ when line.SequenceEqual(idTypes.TRACK) => EntityType.Track,
//                         _ when line.SequenceEqual(idTypes.ARTIST) => EntityType.Artist,
//                         _ when line.SequenceEqual(idTypes.ALBUM) => EntityType.Album,
//                         _ when line.SequenceEqual(idTypes.EPISODE) => EntityType.Episode,
//                         _ when line.SequenceEqual(idTypes.SHOW) => EntityType.Show,
//                         _ when line.SequenceEqual(idTypes.PLAYLIST) => EntityType.Playlist,
//                         _ when line.SequenceEqual(idTypes.COLLECTION) => EntityType.Collection,
//                         _ => EntityType.Unknown
//                     };
//                     break;
//                 }
//                 case 2:
//                     if (Type != EntityType.Unknown)
//                     {
//                         Id = line.ToString();
//                     }
//                     break;
//                 default:
//                     throw new NotImplementedException();
//             }
//
//             i++;
//         }
//     }
//
//     public bool IsValidId => Type != EntityType.Unknown && !string.IsNullOrEmpty(Id);
//     public string Uri { get; }
//     public EntityType Type { get; }
//     public string? Id { get; }
//
//
//     public override int GetHashCode()
//     {
//         return Uri != null ? Uri.GetHashCode() : 0;
//     }
//     private static EntityType GetType(ReadOnlySpan<char> r,
//         string uri)
//     {
//         switch (r)
//         {
//             case var end_group when
//                 end_group.SequenceEqual("station".AsSpan()):
//                 return EntityType.Station;
//             case var track when
//                 track.SequenceEqual("track".AsSpan()):
//                 return EntityType.Track;
//             case var artist when
//                 artist.SequenceEqual("artist".AsSpan()):
//                 return EntityType.Artist;
//             case var album when
//                 album.SequenceEqual("album".AsSpan()):
//                 return EntityType.Album;
//             case var show when
//                 show.SequenceEqual("show".AsSpan()):
//                 return EntityType.Show;
//             case var episode when
//                 episode.SequenceEqual("episode".AsSpan()):
//                 return EntityType.Episode;
//             case var playlist when
//                 playlist.SequenceEqual("playlist".AsSpan()):
//                 return EntityType.Playlist;
//             case var collection when
//                 collection.SequenceEqual("collection".AsSpan()):
//                 return EntityType.Collection;
//             default:
//                 return EntityType.Unknown;
//         }
//     }
//
//     public int Compare(SpotifyId x, SpotifyId y)
//     {
//         return string.Compare(x.Uri, y.Uri, StringComparison.Ordinal);
//     }
//
//     public int CompareTo(SpotifyId other)
//     {
//         return string.Compare(Uri, other.Uri, StringComparison.Ordinal);
//     }
//     public bool Equals(SpotifyId other)
//     {
//         return Uri == other.Uri;
//     }
//
//     public override bool Equals(object obj)
//     {
//         return obj is SpotifyId other && Equals(other);
//     }
//
//     public static bool operator ==(SpotifyId left, SpotifyId right)
//     {
//         return left.Equals(right);
//     }
//
//     public static bool operator !=(SpotifyId left, SpotifyId right)
//     {
//         return !left.Equals(right);
//     }
// }

public readonly struct SpotifyId : IComparable<SpotifyId>, IEquatable<SpotifyId>
{
    public static SpotifyId With(string id, EntityType type)
    {
        return new($"spotify:{type.ToString().ToLower()}:{id}");
    }

    public SpotifyId WithType(EntityType type)
    {
        unsafe
        {
            return new(
                $"spotify:{type.ToString().ToLower()}:{new string(Id)}");
        }
    }

    public SpotifyId(string uri)
    {
        IsValidId = false;
        Uri = uri;
        var s =
            uri.SplitLines();

        var i = 0;
        while (s.MoveNext())
        {
            switch (i)
            {
                case 1:
                    Type = (EntityType)GetType(s.Current.Line);
                    break;
            }
            
            i++;
        }

        Id = s.Current.Line.ToString();
        IsValidId = true;
    }
    public string Uri { get; }
    public EntityType Type { get; }
    public string? Id { get; }

    public SpotifyId(ReadOnlySpan<char> span) : this(span.ToString())
    {
    }

    private static Base62Test _base62 = Base62Test.CreateInstanceWithInvertedCharacterSet();

    public SpotifyId(ByteString trackGid, EntityType track)
    {
        //builder.setUri(uriPrefix + new String(PlayableId.BASE62.encode(track.getGid().toByteArray(), 22)));
        var id = Encoding.UTF8.GetString(_base62.Encode(trackGid.ToByteArray()), 0, 22);
        Uri = $"spotify:{track.ToString().ToLower()}:{id}";
        Id = id;
        Type = track;
        IsValidId = true;
    }

    public bool IsValidId { get; }
    public bool IsLocalId { get; }

    public string HexId()
    {
        //Utils.bytesToHex(BASE62.decode(id.getBytes(), 16))
        var decoded = Id.FromBase62(true);
        var hex = BitConverter.ToString(decoded).Replace("-", string.Empty);
        if (hex.Length > 32)
        {
            hex = hex.Substring(hex.Length - 32, hex.Length - (hex.Length - 32));
        }

        return hex.ToLower();
    }

    public override int GetHashCode()
    {
        return Uri != null ? Uri.GetHashCode() : 0;
    }


    private static int GetType(ReadOnlySpan<char> r)
    {
        return r switch
        {
            "station" => 2,
            "track" => 3,
            "artist" => 1,
            "album" => 4,
            "show" => 5,
            "episode" => 6,
            "playlist" => 7,
            "collection" => 8,
            "user" => 9,
            "local" => 10,
            _ => 0,
        };
    }

    public int Compare(SpotifyId x, SpotifyId y)
    {
        unsafe
        {
            return string.Compare(new string(x.Uri), new string(y.Uri), StringComparison.Ordinal);
        }
    }

    public int CompareTo(SpotifyId other)
    {
        unsafe
        {
            return string.Compare(new string(Uri), new string(other.Uri),
                StringComparison.Ordinal);
        }
    }

    public bool Equals(SpotifyId other)
    {
        unsafe
        {
            return new string(Uri) == new string(other.Uri);
        }
    }

    public override bool Equals(object obj)
    {
        return obj is SpotifyId other && Equals(other);
    }

    public static bool operator ==(SpotifyId left, SpotifyId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SpotifyId left, SpotifyId right)
    {
        return !left.Equals(right);
    } // }
    public static EntityType InferUriType(string contextUri)
    {
        if (contextUri.StartsWith("spotify:episode:") || contextUri.StartsWith("spotify:show:"))
            return EntityType.Episode;
        else
            return EntityType.Track;
    }

    public static string InferUriPrefix(string contextUri) {
        if (contextUri.StartsWith("spotify:episode:") || contextUri.StartsWith("spotify:show:"))
            return "spotify:episode:";
        else
            return "spotify:track:";
    }

    public bool Matches(ContextTrack current)
    {
        if (current.HasUri)
            return Uri == current.Uri;
        else if (current.HasGid)
            return current.Gid.Equals(this.Gid);
        else
            return false;
    }

    public ByteString Gid => ByteString.CopyFrom(Id.FromBase62(true));
}

public static class StringExtensions
{
    public static LineSplitEnumerator SplitLines(this string str)
    {
        // LineSplitEnumerator is a struct so there is no allocation here
        return new LineSplitEnumerator(str.AsSpan());
    }

    // Must be a ref struct as it contains a ReadOnlySpan<char>
    public ref struct LineSplitEnumerator
    {
        private ReadOnlySpan<char> _str;

        public LineSplitEnumerator(ReadOnlySpan<char> str)
        {
            _str = str;
            Current = default;
        }

        // Needed to be compatible with the foreach operator
        public LineSplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var span = _str;
            if (span.Length == 0) // Reach the end of the string
                return false;

            var index = span.IndexOfAny(':', ':');
            if (index == -1) // The string is composed of only one line
            {
                _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
                return true;
            }

            if (index < span.Length - 1 && span[index] == ':')
            {
                // Try to consume the '\n' associated to the '\r'
                var next = span[index + 1];
                if (next == ':')
                {
                    Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 2));
                    _str = span.Slice(index + 2);
                    return true;
                }
            }

            Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 1));
            _str = span.Slice(index + 1);
            return true;
        }

        public bool Move(int count)
        {
            int moved = 0;
            while (moved < count && MoveNext())
            {
                moved++;
            }

            return true;
        }

        public LineSplitEntry Current { get; private set; }
    }

    public readonly ref struct LineSplitEntry
    {
        public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
        {
            Line = line;
            Separator = separator;
        }

        public ReadOnlySpan<char> Line { get; }
        public ReadOnlySpan<char> Separator { get; }

        // This method allow to deconstruct the type, so you can write any of the following code
        // foreach (var entry in str.SplitLines()) { _ = entry.Line; }
        // foreach (var (line, endOfLine) in str.SplitLines()) { _ = line; }
        // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/deconstruct?WT.mc_id=DT-MVP-5003978#deconstructing-user-defined-types
        public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
        {
            line = Line;
            separator = Separator;
        }

        // This method allow to implicitly cast the type into a ReadOnlySpan<char>, so you can write the following code
        // foreach (ReadOnlySpan<char> entry in str.SplitLines())
        public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
    }
}