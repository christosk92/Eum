using System.ComponentModel.DataAnnotations;

namespace Eum.Cores.Apple.Contracts.Models;

public sealed class TokenData
{
    [Required]
    public string TokenValue { get; init; } = null!;

    public DateTimeOffset ExpiresAt { get; init; }

    public bool HasExpired => DateTimeOffset.Now >= ExpiresAt;
}