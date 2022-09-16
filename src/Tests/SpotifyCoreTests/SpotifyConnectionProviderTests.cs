using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Factories;
using NSubstitute;
using Xunit;

namespace SpotifyCoreTests;

public class SpotifyConnectionProviderTests
{
    private readonly ISpotifyConnectionProvider _sut;
    private readonly ISpotifyConnectionFactory _connectionFactory = Substitute.For<ISpotifyConnectionFactory>();
    private readonly ILoginCredentialsProvider _loginCredentialsProvider = Substitute.For<ILoginCredentialsProvider>();
    private readonly ISpotifyConnection _spotifyConnection = Substitute.For<ISpotifyConnection>();
    public SpotifyConnectionProviderTests()
    {
        _sut = new SpotifyTcpConnectionProvider(_loginCredentialsProvider, _connectionFactory);
    }
    [Fact]
    public async Task If_AliveConnection_Return_ThatConnection()
    {
        _connectionFactory.GetNewConnection()
            .Returns(_ => _spotifyConnection);
        var newConnection = await _sut.GetConnectionAsync(CancellationToken.None);

        var getConnection = await _sut.GetConnectionAsync(CancellationToken.None);

        Assert.Equal(newConnection.ConnectionId, getConnection.ConnectionId);
    }
    [Fact]
    public async Task If_NonAliveConnection_Return_NewConnection()
    {

        _spotifyConnection.IsAlive.Returns(true);
        _connectionFactory.GetNewConnection()
            .Returns(_ => _spotifyConnection);
        
        var getConnection = await _sut.GetConnectionAsync(CancellationToken.None);
        Assert.Equal(getConnection.IsAlive, true);
    }
}