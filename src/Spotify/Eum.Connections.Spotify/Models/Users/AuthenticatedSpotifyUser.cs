using System.Collections.Generic;
using Eum.Spotify;

namespace Eum.Connections.Spotify.Models.Users;

public record AuthenticatedSpotifyUser
{
   // public SpotifyPrivateUser User { get; init; }
   
    public string Username { get; init; }
    public IReadOnlyDictionary<string, string> ProductInfo { get; init; }
    public string CountryCode { get; init; }
    public string ReusableAuthCredentialsBase64 { get; init; }
    public AuthenticationType ResuableCredentialsType { get; set; }
}