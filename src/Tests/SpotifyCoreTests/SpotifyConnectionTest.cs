using Eum.Cores.Spotify.Connection;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Factories;
using NSubstitute;

namespace SpotifyCoreTests;

public class SpotifyConnectionTest
{
    private readonly ITcpConnection _tcpConnection = Substitute.For<ITcpConnection>();
    private readonly IApResolver _apResolver = Substitute.For<IApResolver>();
    private readonly ISpotifyConnection _sut;
    public SpotifyConnectionTest()
    {
        _sut = new SpotifyTcpConnection(_apResolver);
    }

    [Fact]
    public async Task T()
    {
        const string host = "test";
        const ushort port = 80;

        _apResolver.GetClosestAccessPoint()
            .Returns(_ => (host, port));
        
        await _sut.InstantiateConnectionAsync(CancellationToken.None);
    }
}