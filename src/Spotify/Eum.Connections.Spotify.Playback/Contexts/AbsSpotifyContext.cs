using System;
using System.Text.RegularExpressions;
using Eum.Connections.Spotify.Playback.Contexts;
using Eum.Connections.Spotify.Playback.States;

namespace Eum.Connections.Spotify.Playback.Contexts;

public abstract class AbsSpotifyContext
{
    public readonly RestrictionsManager Restrictions;
    protected readonly string context;

    protected AbsSpotifyContext(string context)
    {
        this.context = context;
        Restrictions = new RestrictionsManager(this);
    }

    public static bool IsCollection(string uri)
    {
        //could be: spotify:user:{name}:collection:{type}
        //or spotify:collection:{type}
        //do a regex to match
        var isCollection =
            Regex.IsMatch(uri, @"spotify:(user:[^:]+:)?collection:.*");
        return isCollection;
    }

    public static AbsSpotifyContext From(string context)
    {
        if (context.StartsWith("spotify:dailymix:") || context.StartsWith("spotify:station:"))
            return new GeneralInfiniteContext(context);
        else if (context.StartsWith("spotify:search:"))
            return new SearchContext(context, context[15..]);
        else
            return new GeneralFiniteContext(context);
    }

    public override string ToString()
    {
        return "AbsSpotifyContext{context='" + context + "'}";
    }

    public abstract bool IsFinite { get; }

    public string Uri => context;
}

public class UnsupportedContextException : Exception
{
    public UnsupportedContextException(string msg) : base(msg)
    {
    }

    public static UnsupportedContextException CannotPlayAnything =>
        new UnsupportedContextException("Nothing from this context can or should be played!");
}