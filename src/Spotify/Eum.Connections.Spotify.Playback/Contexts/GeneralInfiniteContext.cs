namespace Eum.Connections.Spotify.Playback.Contexts;

public class GeneralInfiniteContext : AbsSpotifyContext
{
    public GeneralInfiniteContext(string s) : base(s)
    {
        
    }

    public override bool IsFinite => false;
}