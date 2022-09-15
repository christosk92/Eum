using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Eum.Core.Contracts.Models;

namespace Eum.Cores.Apple.Models.CoreImplementations;

public sealed class AppleMusicArtwork : IArtwork
{
    /// <summary>
    /// The average background color of the image.
    /// </summary>
    [JsonPropertyName("bgColor")]
    public string? BackgroundColor { get; init; }

    /// <inheritdoc />
    [Required]
    public ushort Height { get; init; }
    
    /// <inheritdoc />
    [Required]
    public uint Width { get; init; }

    /// <summary>
    /// (Required) The URL to request the image asset. {w}x{h}must precede image filename, as placeholders for the width and height values as described above. For example, {w}x{h}bb.jpeg
    /// </summary>
    [Required]
    public string Url { get; init; } = null!;
}