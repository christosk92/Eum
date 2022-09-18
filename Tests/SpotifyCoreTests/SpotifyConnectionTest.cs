using AutoFixture;
using CPlayerLib;
using Eum.Cores.Spotify.Connection;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Factories;
using NSubstitute;

namespace SpotifyCoreTests;

public class SpotifyConnectionTest
{
    private readonly ITcpConnectionFactory _tcpConnectionFactory = Substitute.For<ITcpConnectionFactory>();
    private readonly IApResolver _apResolver = Substitute.For<IApResolver>();
    private readonly IFixture _fixture = new Fixture();
    private readonly LoginCredentials _loginCredentials;

    private readonly ISpotifyConnection _sut;
    
    public SpotifyConnectionTest()
    {
        _loginCredentials = _fixture.Build<LoginCredentials>()
            .Create();
        _sut = new SpotifyConnection(_apResolver, _loginCredentials, _tcpConnectionFactory);
    }
    
}