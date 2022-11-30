namespace Eum.Connections.Spotify.Playback.Contexts;

public class GeneralFiniteContext : AbsSpotifyContext
{
    public GeneralFiniteContext(string s) : base(s)
    {
    }

    public override bool IsFinite => true;
}