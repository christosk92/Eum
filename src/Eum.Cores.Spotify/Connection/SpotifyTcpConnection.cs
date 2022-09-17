using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using CPlayerLib;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Crypto;
using Eum.Cores.Spotify.Exceptions;
using Eum.Cores.Spotify.Helpers;
using Google.Protobuf;

namespace Eum.Cores.Spotify.Connection;

public sealed class SpotifyTcpConnection : ISpotifyConnection
{
    private TcpClient? _tcpClient;
    private readonly IApResolver _apResolver;

    public SpotifyTcpConnection(IApResolver apResolver)
    {
        _apResolver = apResolver;
        ConnectionId = Guid.NewGuid();
    }

    public Guid ConnectionId { get; }

    public bool IsAlive
    {
        get
        {
            try
            {
                return _tcpClient is
                {
                    Connected: true
                } && _tcpClient.GetStream().ReadTimeout > -2;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }


    public bool IsAuthenticated => IsAlive && didSupplyCredentials;

    public async Task<(APResponseMessage apResponseMessage, byte[] sharedKey, MemoryStream accumulator)> HandshakeAsync(
        CancellationToken ct = default)
    {
        var (host, port)
            = await _apResolver.GetClosestAccessPoint(ct);

        _tcpClient = new TcpClient(host, port)
        {
            ReceiveTimeout = 500
        };

        var keys = new DiffieHellman();

        _tcpClient.GetStream().WriteByte(0x00);
        _tcpClient.GetStream().WriteByte(0x04);
        _tcpClient.GetStream().WriteByte(0x00);
        _tcpClient.GetStream().WriteByte(0x00);
        _tcpClient.GetStream().WriteByte(0x00);
        await _tcpClient.GetStream().FlushAsync(ct);


        var clientHello = GetClientHello(keys);
        var clientHelloBytes = clientHello.ToByteArray();

        var length = 2 + 4 + clientHelloBytes.Length;
        var bytes = BitConverter.GetBytes(length);

        _tcpClient.GetStream().WriteByte(bytes[0]);
        await _tcpClient.GetStream().WriteAsync(clientHelloBytes, 0, clientHelloBytes.Length, ct);
        await _tcpClient.GetStream().FlushAsync(ct);

        var buffer = new byte[1000];

        var len = int.Parse((await _tcpClient.GetStream().ReadAsync(buffer,
            0, buffer.Length, ct)).ToString());
        var tmp = new byte[len];
        Array.Copy(buffer, tmp, len);

        tmp = tmp.Skip(4).ToArray();
        var accumulator = new MemoryStream();
        accumulator.WriteByte(0x00);
        accumulator.WriteByte(0x04);

        var lnarr = length.ToByteArray();
        await accumulator.WriteAsync(lnarr, 0, lnarr.Length, ct);
        await accumulator.WriteAsync(clientHelloBytes, 0, clientHelloBytes.Length, ct);

        var lenArr = len.ToByteArray();
        await accumulator.WriteAsync(lenArr, 0, lenArr.Length, ct);
        await accumulator.WriteAsync(tmp, 0, tmp.Length, ct);
        accumulator.Position = 0;

        var apResponseMessage = APResponseMessage.Parser.ParseFrom(tmp);


        var sharedKey = ByteExtensions.ToByteArray(keys.ComputeSharedKey(apResponseMessage
            .Challenge.LoginCryptoChallenge.DiffieHellman.Gs.ToByteArray()));
        return (apResponseMessage, sharedKey, accumulator);
    }

    public async Task<(IShannon ReceiveCipher, IShannon SendCipher)> SolveChallengeAsync(
        APResponseMessage apResponseMessage,
        byte[] sharedKey,
        MemoryStream accumulator,
        CancellationToken ct = default)
    {
        // Check gs_signature
        var rsa = new RSACryptoServiceProvider();
        var rsaKeyInfo = new RSAParameters
        {
            Modulus = new BigInteger(Consts.ServerKey, true, true).ToByteArray(true, true),
            Exponent = new BigInteger(65537).ToByteArray(true, true)
        };

        //Import key parameters into RSA.
        rsa.ImportParameters(rsaKeyInfo);
        var gs = apResponseMessage.Challenge.LoginCryptoChallenge.DiffieHellman.Gs.ToByteArray();
        var sign = apResponseMessage.Challenge.LoginCryptoChallenge.DiffieHellman.GsSignature.ToByteArray();

        if (!rsa.VerifyData(gs,
                sign,
                HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
            throw new AccessViolationException("Failed to verify APResponse");

        // Solve challenge
        var binaryData = accumulator.ToArray();
        using var data = new MemoryStream();
        var mac = new HMACSHA1(sharedKey);
        mac.Initialize();
        for (var i = 1; i < 6; i++)
        {
            mac.TransformBlock(binaryData, 0, binaryData.Length, null, 0);
            var temp = new[] { (byte)i };
            mac.TransformBlock(temp, 0, temp.Length, null, 0);
            mac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var final = mac.Hash;
            data.Write(final, 0, final.Length);
            mac = new HMACSHA1(sharedKey);
        }

        var dataArray = data.ToArray();
        mac = new HMACSHA1(dataArray.copyOfRange(0, 0x14));
        mac.TransformBlock(binaryData, 0, binaryData.Length, null, 0);
        mac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var challenge = mac.Hash;
        var clientResponsePlaintext = new ClientResponsePlaintext
        {
            LoginCryptoResponse = new LoginCryptoResponseUnion
            {
                DiffieHellman = new LoginCryptoDiffieHellmanResponse
                {
                    Hmac = ByteString.CopyFrom(challenge)
                }
            },
            PowResponse = new PoWResponseUnion(),
            CryptoResponse = new CryptoResponseUnion()
        };
        var clientResponsePlaintextBytes = clientResponsePlaintext.ToByteArray();
        var len2 = 4 + clientResponsePlaintextBytes.Length;

        _tcpClient.GetStream().WriteByte(0x00);
        _tcpClient.GetStream().WriteByte(0x00);
        _tcpClient.GetStream().WriteByte(0x00);
        var bytesb = BitConverter.GetBytes(len2);
        _tcpClient.GetStream().WriteByte(bytesb[0]);
        await _tcpClient.GetStream()
            .WriteAsync(clientResponsePlaintextBytes, 0, clientResponsePlaintextBytes.Length, ct);
        await _tcpClient.GetStream().FlushAsync(ct);

        if (_tcpClient.GetStream().DataAvailable)
            //if data is available, it could be scrap or a failed login.
            try
            {
                var scrap = new byte[4];
                _tcpClient.GetStream().ReadTimeout = 300;
                var read = await _tcpClient.GetStream().ReadAsync(scrap, 0, scrap.Length, ct);
                if (read == scrap.Length)
                {
                    var lengthOfScrap = (scrap[0] << 24) | (scrap[1] << 16) | (scrap[2] << 8) |
                                        (scrap[3] & 0xFF);
                    var payload = await _tcpClient.GetStream().ReadCompleteAsync(ct);
                    var failed = APResponseMessage.Parser.ParseFrom(payload);
                    throw new SpotifyConnectionException(failed);
                }

                if (read > 0) throw new UnknownDataException(scrap);
            }
            catch (Exception x)
            {
                // ignored
            }

        _sendCipher = new Shannon();
        var arra = data.ToArray();
        _sendCipher.Key(arra.copyOfRange(0x14, 0x34));
        //SendCipher.Key(Arrays.CopyOfRange(data.ToArray(), 0x14, 0x34));

        _receiveCipher = new Shannon();
        _receiveCipher.Key(arra.copyOfRange(0x34, 0x54));

        _tcpClient.GetStream().ReadTimeout = Timeout.Infinite;

        await accumulator.DisposeAsync();
        return (_receiveCipher, _sendCipher);
    }


    /// <inheritdoc/>
    public async Task InstantiateConnectionAsync(CancellationToken ct = default)
    {
        if (IsAlive) return;

        var (apResponseMessage, sharedKey, accumulator) = await HandshakeAsync(ct);

        var (send, receive) = await SolveChallengeAsync(apResponseMessage, sharedKey, accumulator, ct);
    }

    public async Task<APWelcome> AuthenticateAsync(LoginCredentials loginCredentials, CancellationToken ct = default)
    {
        if (IsAuthenticated) return _apWelcome;
        ArgumentNullException.ThrowIfNull(loginCredentials);
        await InstantiateConnectionAsync(ct);

        didSupplyCredentials = true;
        return new APWelcome();
    }


    private static ClientHello GetClientHello(DiffieHellman publickey)
    {
        var clientHello = new ClientHello
        {
            BuildInfo = new BuildInfo
            {
                Platform = Platform.Win32X86,
                Product = Product.Client,
                ProductFlags = { ProductFlags.ProductFlagNone },
                Version = 112800721
            }
        };

        clientHello.CryptosuitesSupported.Add(Cryptosuite.Shannon);
        clientHello.LoginCryptoHello = new LoginCryptoHelloUnion
        {
            DiffieHellman = new LoginCryptoDiffieHellmanHello
            {
                Gc = ByteString.CopyFrom(publickey.PublicKeyArray()),
                ServerKeysKnown = 1
            }
        };
        var nonce = new byte[16];
        new Random().NextBytes(nonce);
        clientHello.ClientNonce = ByteString.CopyFrom(nonce);
        clientHello.Padding = ByteString.CopyFrom(30);

        return clientHello;
    }

    private APWelcome _apWelcome;
    private bool didSupplyCredentials;
    private Shannon _receiveCipher;
    private Shannon _sendCipher;

    public void Dispose()
    {
        _tcpClient?.Dispose();
    }
}