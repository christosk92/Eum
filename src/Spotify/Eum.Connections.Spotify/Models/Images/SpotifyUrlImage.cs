using Eum.Artwork;

namespace Eum.Connections.Spotify.Models.Images;

public readonly record struct SpotifyUrlImage(string Url, ushort? Height = 0, uint? Width = 0) : IArtwork;