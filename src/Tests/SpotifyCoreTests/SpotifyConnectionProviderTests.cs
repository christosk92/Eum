using AutoFixture;
using CPlayerLib;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Factories;
using NSubstitute;

namespace SpotifyCoreTests;

public class SpotifyConnectionProviderTests
{
    private readonly ISpotifyConnectionProvider _sut;
    private readonly ISpotifyConnectionFactory _connectionFactory = Substitute.For<ISpotifyConnectionFactory>();
    private readonly ILoginCredentialsProvider _loginCredentialsProvider = Substitute.For<ILoginCredentialsProvider>();

    private readonly IFixture _fixture = new Fixture();
    public SpotifyConnectionProviderTests()
    {
        _sut = new SpotifyConnectionProvider(_loginCredentialsProvider, _connectionFactory);
    }
    [Fact]
    public async Task If_AliveConnection_Return_ThatConnection()
    {
        var loginCredentials = _fixture
            .Build<LoginCredentials>()
            .Create();

        _loginCredentialsProvider.GetCredentials()
            .Returns(loginCredentials);
        
        var connection = Substitute.For<ISpotifyConnection>();
        
        _connectionFactory.GetNewConnection(loginCredentials)
            .Returns(_ => connection);
        
        var newConnection = await _sut.GetConnectionAsync(CancellationToken.None);
        var getConnection = await _sut.GetConnectionAsync(CancellationToken.None);

        Assert.Equal(newConnection.ConnectionId, getConnection.ConnectionId);
    }
    [Fact]
    public async Task If_NonAliveConnection_Return_NewConnection()
    {
        var loginCredentials = _fixture
            .Build<LoginCredentials>()
            .Create();
        
        _loginCredentialsProvider.GetCredentials()
            .Returns(loginCredentials);

        var connection = Substitute.For<ISpotifyConnection>();

        _connectionFactory.GetNewConnection(loginCredentials)
            .Returns(_ => connection);
        
        var getConnection = await 
            _sut.GetConnectionAsync(CancellationToken.None);
        Assert.Equal(getConnection.ConnectionId, connection.ConnectionId);
    }
}