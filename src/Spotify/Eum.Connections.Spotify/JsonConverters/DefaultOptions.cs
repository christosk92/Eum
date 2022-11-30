using System.Text.Json;
using Refit;

namespace Eum.Connections.Spotify.JsonConverters;

public static class DefaultOptions
{
    static DefaultOptions()
    {
        Default = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
        RefitSettings = new SystemTextJsonContentSerializer(Default);
    }
    
    public static SystemTextJsonContentSerializer RefitSettings { get; }
    public static JsonSerializerOptions Default { get; }
}