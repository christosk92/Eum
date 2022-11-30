using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Eum.Connections.Spotify.Events
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public abstract class Enumeration : IComparable
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public string Name { get; private set; }

        public int Id { get; private set; }

        public int Unknown{ get; private set; }
        protected Enumeration(int id, int unknown, string name)
        {
            Id = id;
            Name = name;
            Unknown = unknown;
        }

        public override string ToString() => Name;

        public static IEnumerable<T> GetAll<T>() where T : Enumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public |
                                             BindingFlags.Static |
                                             BindingFlags.DeclaredOnly);

            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        public override bool Equals(object obj)
        {
            var otherValue = obj as Enumeration;

            if (otherValue == null)
                return false;

            var typeMatches = GetType() == obj.GetType();
            var valueMatches = Id.Equals(otherValue.Id);

            return typeMatches && valueMatches;
        }

        public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);

        // Other utility methods ...
    }

    public class EventType : Enumeration
    {
        public static readonly EventType LANGUAGE = new EventType(812, 1, "LANGUAGE");
        public static readonly EventType FETCHED_FILE_ID = new EventType(247, 3, "FETCHED_FILE_ID");
        public static readonly EventType NEW_SESSION_ID = new EventType(557, 3,"NEW_SESSION_ID");
        public static readonly EventType NEW_PLAYBACK_ID = new EventType(558, 1, "NEW_PLAYBACK_ID");
        public static readonly EventType TRACK_PLAYED = new EventType(372, 1, "TRACK_PLAYED");
        public static readonly EventType TRACK_TRANSITION = new EventType(12, 37, "TRACK_TRANSITION");
        public static readonly EventType CDN_REQUEST = new EventType(10, 20,"CDN_REQUEST");

        public EventType(int id, 
            int unknown,
            string name)
            : base(id, unknown, name)
        {
        }
    }
}