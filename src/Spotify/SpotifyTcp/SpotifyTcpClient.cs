using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Nito.AsyncEx;
using SpotifyTcp.Contracts;
using SpotifyTcp.Crypto;
using System.Numerics;
using System.Security.Cryptography;
using SpotifyTcp.Helpers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Logging;
using Eum.Spotify;
using SpotifyTcp.Exceptions;
using SpotifyTcp.Models;

namespace SpotifyTcp;

public sealed class SpotifyTcpClient : ISpotifyTcpClient, IMissingAuthenticationSpotifyConnection
{
    private AsyncLock _handshakeLock = new AsyncLock();
    private readonly TcpClient _tcpClient;
    private bool _didHandshake;

    public SpotifyTcpClient(string host, int port)
    {
        _tcpClient = new TcpClient(host, port)
        {
            ReceiveTimeout = 500,
            ReceiveBufferSize = 32 * 1024
        };
    }

    public Guid ConnectionId { get; } = Guid.NewGuid();

    public async Task<IMissingAuthenticationSpotifyConnection?> ConnectAsync(CancellationToken ct = default)
    {
        using (await _handshakeLock.LockAsync(ct))
        {
            if (_didHandshake)
                return null;
            S_Log.Instance.LogInfo($"Starting connection...");
            var sw_main = Stopwatch.StartNew();
            var local_keys = new DiffieHellman();
            var gc = local_keys.PublicKeyArray();

            using var accumulator = new MemoryStream();
            var sw_cleint_hello = Stopwatch.StartNew();
            await client_hello(gc, accumulator, ct);

            S_Log.Instance.LogInfo($"Received client_hello, took exactly: {sw_cleint_hello.Elapsed}" +
                                   $"received exactly  {accumulator.Length} bytes...");

            sw_cleint_hello.Stop();

            var sw_ap = Stopwatch.StartNew();
            var apResponseMessage = await get_ap_response(accumulator, ct);
            sw_ap.Stop();
            S_Log.Instance.LogInfo($"Received APResponse, took exactly: {sw_ap.Elapsed} " +
                                   $" {apResponseMessage}...");
            S_Log.Instance.LogInfo(JsonSerializer.Serialize(apResponseMessage));

            var sw_verify = Stopwatch.StartNew();
            var remoteKey = apResponseMessage
                .Challenge
                .LoginCryptoChallenge
                .DiffieHellman
                .Gs
                .ToByteArray();

            var remoteSignature = apResponseMessage
                .Challenge
                .LoginCryptoChallenge
                .DiffieHellman
                .GsSignature
                .ToByteArray();

            // Prevent man-in-the-middle attacks: check server signature
            var n = new BigInteger(ServerKey, true, true)
                .ToByteArray(true, true);
            var e = new BigInteger(65537).ToByteArray(true, true);

            using var rsa = new RSACryptoServiceProvider();
            var rsaKeyInfo = new RSAParameters
            {
                Modulus = n,
                Exponent = e
            };
            rsa.ImportParameters(rsaKeyInfo);
            var gs = apResponseMessage.Challenge.LoginCryptoChallenge.DiffieHellman.Gs.ToByteArray();
            var sign = apResponseMessage.Challenge.LoginCryptoChallenge.DiffieHellman.GsSignature.ToByteArray();

            if (!rsa.VerifyData(gs,
                    sign,
                    HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
            {
                S_Log.Instance.LogWarning("Failed to verify APResponse");
                throw new AccessViolationException("Failed to verify APResponse");
            }

            //ok to proceed
            var shared_secret = local_keys
                .ComputeSharedKey(remoteKey);

            var (challenge, send_key, recv_key) =
                compute_keys(shared_secret, accumulator);

            _sendKey = send_key;
            _receiveKey = recv_key;

            sw_verify.Stop();

            var sw_client_response = Stopwatch.StartNew();
            await client_response(challenge, ct);
            _didHandshake = true;
            sw_client_response.Stop();
            S_Log.Instance.LogInfo($"Finished handshake, took exactly: {sw_ap.Elapsed} ");
            sw_main.Stop();

            S_Log.Instance.LogInfo($"Finished connection, took exactly: {sw_main.Elapsed} ");
            return this;
        }
    }

    public async Task<int> SendPacketAsync(MercuryPacket packet, CancellationToken ct = default)
    {
        S_Log.Instance.LogInfo($"{packet.Cmd} Starting send...");
        var sw = Stopwatch.StartNew();
        using (await _sendLock.LockAsync(ct))
        {
            var payload = packet.Payload;
            var cmd = packet.Cmd;

            var sendNonce = Interlocked.Increment(ref _sendCounter);
            var sendCipher = new Shannon(sendNonce, _sendKey);

            var payloadLengthAsByte = BitConverter.GetBytes((short)payload.Length).Reverse().ToArray();
            using var yetAnotherBuffer = new MemoryStream(3 + payload.Length);
            yetAnotherBuffer.WriteByte((byte)cmd);
            await yetAnotherBuffer.WriteAsync(payloadLengthAsByte, ct);
            await yetAnotherBuffer.WriteAsync(payload, ct);


            var bufferBytes = yetAnotherBuffer.ToArray();
            sendCipher.Encrypt(bufferBytes);

            var fourBytesBuffer = new byte[4];
            sendCipher.Finish(fourBytesBuffer);

            var networkStream = _tcpClient.GetStream();
            await networkStream.WriteAsync(bufferBytes, ct);
            await networkStream.WriteAsync(fourBytesBuffer, ct);
            await networkStream.FlushAsync(ct);
            sw.Stop();
            S_Log.Instance.LogInfo($"{packet.Cmd} took {sw.Elapsed} to send");
            return sendNonce;
        }
    }

    private AsyncLock _receiveLock = new AsyncLock();
    private AsyncLock _sendLock = new AsyncLock();

    public async Task<MercuryPacket> ReceivePacketAsync(Shannon cipher, CancellationToken ct = default)
    {
        using (await _receiveLock.LockAsync(ct))
        {
            var sw = Stopwatch.StartNew();
            S_Log.Instance.LogInfo($"Requested package with nonce {cipher.Nonce}");

            var networkStream = _tcpClient.GetStream();
            var previousTimeout = _tcpClient.GetStream().ReadTimeout;
            _tcpClient.ReceiveBufferSize = 32 * 1024;
            networkStream.ReadTimeout = Timeout.Infinite;
            var headerBytes = new byte[3];
            await networkStream.ReadCompleteAsync(headerBytes, 0,
                3, ct);
            cipher.Decrypt(headerBytes);

            var cmd = headerBytes[0];
            var payloadLength = (short)((headerBytes[1] << 8) | (headerBytes[2] & 0xFF));

            var payloadBytes = payloadLength > 0 ? new byte[payloadLength] : Array.Empty<byte>();
            S_Log.Instance.LogInfo($"[{cipher.Nonce}] Received header, cmd: {cmd}, payload length: {payloadLength}");
            if (payloadLength > 0)
            {
                await networkStream.ReadCompleteAsync(payloadBytes, 0, payloadLength, ct);
            }

            cipher.Decrypt(payloadBytes);

            var mac = new byte[4];
            await networkStream.ReadCompleteAsync(mac, 0, 4, ct);

            var expectedMac = new byte[4];
            cipher.Finish(expectedMac);
            networkStream.ReadTimeout = previousTimeout;

            S_Log.Instance.LogInfo(
                $"Received package with nonce {cipher.Nonce}, cmd {cmd}, payload length {payloadLength}," +
                $" mac {mac.BytesToHex()}, " +
                $"expected mac {expectedMac.BytesToHex()}" +
                $" took {sw.Elapsed} since started listening");
            return new MercuryPacket((MercuryPacketType)cmd, payloadBytes);
        }
    }

    public async Task<MercuryPacket> WaitForPacketAsync(CancellationToken ct = default)
    {
        var nonce = Interlocked.Increment(ref _receiveCounter);
        var receiveCipher = new Shannon(nonce, _receiveKey);

        var packet = await ReceivePacketAsync(receiveCipher, ct);
        return packet;
    }

    public void Disconnect()
    {
        _tcpClient
            .GetStream().Close();
    }

    public bool IsAlive
    {
        get
        {
            try
            {
                return _tcpClient.Connected && _didHandshake
                    && _tcpClient.GetStream().CanWrite;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }

    public void Dispose()
    {
        _tcpClient.Dispose();
    }


    /// <summary>
    /// Send the client-hello request to the sever, and returns the accumulated bytes on the response.
    /// This is needed to check the server signature.
    /// </summary>
    /// <param name="gc"></param>
    /// <param name="accumulator"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    private async Task client_hello(byte[] gc,
        Stream accumulator,
        CancellationToken ct = default)
    {
        static Platform GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Platform.OsxX8664;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Platform.LinuxX8664;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Platform.Win32X86;
            }

            return Platform.LinuxX8664;
        }

        var platform = GetOperatingSystem();

        var platformFlags = ProductFlags.ProductFlagDevBuild;

        var nonce = new byte[16];
        new Random().NextBytes(nonce);
        var packet = new ClientHello
        {
            BuildInfo = new BuildInfo
            {
                Product = Product.Client,
                ProductFlags =
                {
                    platformFlags
                },
                Platform = platform,
                Version = 11,
            },
            CryptosuitesSupported = { Cryptosuite.Shannon },
            LoginCryptoHello = new LoginCryptoHelloUnion
            {
                DiffieHellman = new LoginCryptoDiffieHellmanHello
                {
                    Gc = ByteString.CopyFrom(gc),
                    ServerKeysKnown = 1
                }
            },
            ClientNonce = ByteString.CopyFrom(nonce),
            Padding = ByteString.CopyFrom(30)
        };

        var networkStream = _tcpClient.GetStream();
        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x04);
        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x00);
        await networkStream.FlushAsync(ct);


        var buffer = packet.ToByteArray();
        var length = 2 + 4 + buffer.Length;
        var bytes = BitConverter.GetBytes(length);

        networkStream.WriteByte(bytes[0]);
        await networkStream.WriteAsync(buffer, 0, buffer.Length, ct);
        await networkStream.FlushAsync(ct);

        accumulator.WriteByte(0x00);
        accumulator.WriteByte(0x04);

        var lnarr = length.ToByteArray();
        await accumulator.WriteAsync(lnarr, ct);
        await accumulator.WriteAsync(buffer, ct);
    }
    private async Task<APResponseMessage> get_ap_response(MemoryStream accumulator,
        CancellationToken ct = default)
    {
        var networkStream = _tcpClient.GetStream();

        var buffer = new byte[8096];
        var len = await networkStream.ReadAsync(buffer,
            0, buffer.Length, ct);


        var tmp = new byte[len];
        Array.Copy(buffer, tmp, len);
        tmp = tmp.Skip(4).ToArray();
        var lenArr = len.ToByteArray();


        accumulator.Write(lenArr, 0, lenArr.Length);
        accumulator.Write(tmp, 0, tmp.Length);
        accumulator.Position = 0;

        var apresponse =
            APResponseMessage.Parser.ParseFrom(tmp);

        return apresponse;
    }

    private async Task client_response(byte[] challenge,
        CancellationToken ct = default)
    {
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
        var len = 4 + clientResponsePlaintextBytes.Length;

        var networkStream = _tcpClient.GetStream();
        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x00);
        var bytesb = BitConverter.GetBytes(len);
        networkStream.WriteByte(bytesb[0]);
        await networkStream.WriteAsync(clientResponsePlaintextBytes, 0, clientResponsePlaintextBytes.Length, ct);
        await networkStream.FlushAsync(ct);

        if (networkStream.DataAvailable)
            //if data is available, it could be scrap or a failed login.
            try
            {
                var scrap = new byte[4];
                networkStream.ReadTimeout = 300;
                var read = await networkStream.ReadAsync(scrap, 0, scrap.Length, ct);
                if (read == scrap.Length)
                {
                    var lengthOfScrap = (scrap[0] << 24) | (scrap[1] << 16) | (scrap[2] << 8) |
                                        (scrap[3] & 0xFF);
                    var payload = new byte[lengthOfScrap]; 
                    await networkStream.ReadCompleteAsync(payload, 0, lengthOfScrap, ct: ct);
                    var failed = APResponseMessage.Parser.ParseFrom(payload);
                    throw new SpotifyConnectionException(failed);
                }

                if (read > 0) throw new UnknownDataException(scrap);
            }
            catch (Exception x)
            {
                // ignored
            }
    }
    private (byte[] challenge, byte[] send_key, byte[] recv_key) compute_keys(
        BigInteger sharedSecret, MemoryStream accumulator)
    {

        var sharedKey = ByteExtensions.ToByteArray(sharedSecret);
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

        var allkeys = data.ToArray();

        return (challenge, allkeys.copyOfRange(0x14, 0x34),
            allkeys.copyOfRange(0x34, 0x54))!;
    }


    internal static readonly byte[] ServerKey =
    {
        0xac, 0xe0, 0x46, 0x0b, 0xff, 0xc2, 0x30, 0xaf,
        0xf4, 0x6b, 0xfe, 0xc3,
        0xbf, 0xbf, 0x86, 0x3d, 0xa1, 0x91, 0xc6, 0xcc,
        0x33, 0x6c, 0x93, 0xa1,
        0x4f, 0xb3, 0xb0, 0x16, 0x12, 0xac, 0xac, 0x6a,
        0xf1, 0x80, 0xe7, 0xf6,
        0x14, 0xd9, 0x42, 0x9d, 0xbe, 0x2e, 0x34, 0x66,
        0x43, 0xe3, 0x62, 0xd2,
        0x32, 0x7a, 0x1a, 0x0d, 0x92, 0x3b, 0xae, 0xdd,
        0x14, 0x02, 0xb1, 0x81,
        0x55, 0x05, 0x61, 0x04, 0xd5, 0x2c, 0x96, 0xa4,
        0x4c, 0x1e, 0xcc, 0x02,
        0x4a, 0xd4, 0xb2, 0x0c, 0x00, 0x1f, 0x17, 0xed,
        0xc2, 0x2f, 0xc4, 0x35,
        0x21, 0xc8, 0xf0, 0xcb, 0xae, 0xd2, 0xad, 0xd7,
        0x2b, 0x0f, 0x9d, 0xb3,
        0xc5, 0x32, 0x1a, 0x2a, 0xfe, 0x59, 0xf3, 0x5a,
        0x0d, 0xac, 0x68, 0xf1,
        0xfa, 0x62, 0x1e, 0xfb, 0x2c, 0x8d, 0x0c, 0xb7,
        0x39, 0x2d, 0x92, 0x47,
        0xe3, 0xd7, 0x35, 0x1a, 0x6d, 0xbd, 0x24, 0xc2,
        0xae, 0x25, 0x5b, 0x88,
        0xff, 0xab, 0x73, 0x29, 0x8a, 0x0b, 0xcc, 0xcd,
        0x0c, 0x58, 0x67, 0x31,
        0x89, 0xe8, 0xbd, 0x34, 0x80, 0x78, 0x4a, 0x5f,
        0xc9, 0x6b, 0x89, 0x9d,
        0x95, 0x6b, 0xfc, 0x86, 0xd7, 0x4f, 0x33, 0xa6,
        0x78, 0x17, 0x96, 0xc9,
        0xc3, 0x2d, 0x0d, 0x32, 0xa5, 0xab, 0xcd, 0x05,
        0x27, 0xe2, 0xf7, 0x10,
        0xa3, 0x96, 0x13, 0xc4, 0x2f, 0x99, 0xc0, 0x27,
        0xbf, 0xed, 0x04, 0x9c,
        0x3c, 0x27, 0x58, 0x04, 0xb6, 0xb2, 0x19, 0xf9,
        0xc1, 0x2f, 0x02, 0xe9,
        0x48, 0x63, 0xec, 0xa1, 0xb6, 0x42, 0xa0, 0x9d,
        0x48, 0x25, 0xf8, 0xb3,
        0x9d, 0xd0, 0xe8, 0x6a, 0xf9, 0x48, 0x4d, 0xa1,
        0xc2, 0xba, 0x86, 0x30,
        0x42, 0xea, 0x9d, 0xb3, 0x08, 0x6c, 0x19, 0x0e,
        0x48, 0xb3, 0x9d, 0x66,
        0xeb, 0x00, 0x06, 0xa2, 0x5a, 0xee, 0xa1, 0x1b,
        0x13, 0x87, 0x3c, 0xd7,
        0x19, 0xe6, 0x55, 0xbd
    };

    private byte[] _sendKey;
    private byte[] _receiveKey;

    private int _sendCounter = -1;
    private int _receiveCounter = -1;
    private AsyncLock _clienSendLock = new AsyncLock();
    private AsyncLock _clientReceiveLock = new AsyncLock();
    
    public bool Equals(SpotifyTcpClient other)
    {
        return ConnectionId.Equals(other.ConnectionId);
    }
    public bool Equals(ISpotifyTcpClient? other)
    {
        return ConnectionId.Equals(other?.ConnectionId);
    }
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is SpotifyTcpClient other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ConnectionId.GetHashCode();
    }
}