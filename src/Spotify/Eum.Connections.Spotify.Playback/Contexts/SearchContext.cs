namespace Eum.Connections.Spotify.Playback.Contexts;

public class SearchContext : GeneralFiniteContext
{
    public readonly string SearchTerm;
    public SearchContext(string s, string s1) : base(s)
    {
        SearchTerm = s1;
    }
}