namespace Eum.Connections.Spotify.Clients.Contracts
{
    public interface IExtractedColorsClient
    {
        ValueTask<IReadOnlyDictionary<ColorTheme, string>> GetColors(string image, CancellationToken ct = default);
    }
}
