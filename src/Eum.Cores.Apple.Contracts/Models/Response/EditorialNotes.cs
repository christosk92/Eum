namespace Eum.Cores.Apple.Contracts.Models.Response;

/// <summary>
/// An object that represents a notes attribute. <br/>
///
/// Notes may include XML tags for formatting (&lt;b&gt; for bold, &lt;i&gt; for italic, or &lt;br&gt; for line break) and special characters (&amp;amp; for &amp;, &amp;lt; for &lt;, &amp;gt; for &gt;, &amp;apos; for ‘, and &amp;quot; for “).
/// </summary>
public sealed class EditorialNotes
{
    /// <summary>
    /// Abbreviated notes shown inline or when the content appears alongside other content.
    /// </summary>
    public string? Short
    {
        get;
        init;
    }

    /// <summary>
    /// Notes shown when the content is prominently displayed.
    /// </summary>
    public string? Standard
    {
        get;
        init;
    }

    /// <summary>
    /// Name for the editorial notes.
    /// </summary>
    public string? Name
    {
        get;
        init;
    }

    /// <summary>
    /// The tag line for the editorial notes.
    /// </summary>
    public string? Tagline
    {
        get;
        init;
    }
}
