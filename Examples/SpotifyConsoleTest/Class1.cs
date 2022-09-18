using BenchmarkDotNet.Attributes;
using CPlayerLib;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Factories;
using Eum.Cores.Spotify.Services;
using Google.Protobuf;

namespace ConnectionBenchmark;

public class ConnectionBenchmark
{
    private readonly LoginCredentials _loginCredentials = new LoginCredentials
    {
        Username = "tak123chris@gmail.com",
        Typ = AuthenticationType.AuthenticationUserPass,
        AuthData = ByteString.CopyFromUtf8("Hyeminseo22")
    };
    private readonly ISpotifyConnectionFactory _connectionFactory;
    public ConnectionBenchmark()
    {
        _connectionFactory = new SpotifyConnectionFactory(new InternalAP(), new TcpConnectionFactory());
    }

    [Benchmark]
    public async Task<ISpotifyConnection> GetConnection()
    {
        var connection = 
            _connectionFactory.GetNewConnection(_loginCredentials);
        await connection.EnsureConnectedAsync();
        return connection;
    }
    
}

public class InternalAP : IApResolver
{
    public Task<(string, ushort)> GetClosestAccessPoint(CancellationToken ct = default)
    {
        return Task.FromResult(("ap-gae2.spotify.com", (ushort)4070));
    }

    public Task<string> GetClosestDealerAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}