using System;

namespace Eum.Connections.Spotify.Attributes;

/// <summary>
///     Specify a Base URL for an entire Refit interface
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class OpenUrlEndpoint
    : Attribute
{
}
/// <summary>
///     Specify a Base URL for an entire Refit interface
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class SpClientEndpoint
    : Attribute
{
}