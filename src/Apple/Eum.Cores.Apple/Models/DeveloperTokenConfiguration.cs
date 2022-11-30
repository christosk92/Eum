using System.ComponentModel.DataAnnotations;

namespace Eum.Cores.Apple.Models;

/// <summary>
/// A config class used to generate developer tokens to authenticate with Apple.
/// </summary>
public sealed class DeveloperTokenConfiguration
{
    /// <summary>
    /// To locate your Team ID, sign in to your developer account, and click Membership in the sidebar. <br/>Your Team ID appears in the Membership Information section under the team name.
    /// </summary>
    [Required]
    public string TeamId { get; set; } = null!;

    /// <summary>
    /// A 10-character key identifier (kid) key, obtained from your developer account
    /// </summary>
    [Required]
    public string KeyId { get; set; } = null!;

    /// <summary>
    /// The path to the p8 key on the physical drive.
    /// </summary>
    [Required]
    public string PathToFile { get; set; } = null!;
    
    public string? DefaultStorefrontId { get; set; }
}