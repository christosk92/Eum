using Eum.Artwork;

namespace Eum.Users;

/// <summary>
/// An object that represents a user.
/// </summary>
public interface IUser
{
    /// <summary>
    /// The display name of the user.
    /// </summary>
    string? Name { get; }
    /// <summary>
    /// The artwork of the user.
    /// </summary>
    IArtwork[] Avatar { get; }

    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    string Id { get; }
}