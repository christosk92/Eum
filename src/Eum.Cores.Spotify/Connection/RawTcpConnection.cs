using System.Diagnostics;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CPlayerLib;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;
using Eum.Cores.Spotify.Contracts.Enums;
using Eum.Cores.Spotify.Contracts.Models;
using Eum.Cores.Spotify.Crypto;
using Eum.Cores.Spotify.Exceptions;
using Eum.Cores.Spotify.Helpers;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace Eum.Cores.Spotify.Connection;

internal sealed class RawTcpConnection : ITcpConnection
{
    private readonly TcpClient tcpClient;

    public RawTcpConnection(TcpClient tcpClient)
    {
        this.tcpClient = tcpClient;
    }

    private AsyncLock _authenticationLock = new AsyncLock();
    private AsyncLock _handshakeLock = new AsyncLock();
    public async Task<bool> HandshakeAsync(CancellationToken ct = default)
    {
        if (_didHandshake)
            return true;
        using (await _handshakeLock.LockAsync(ct))
        {
            var sw_main = Stopwatch.StartNew();
            var local_keys = new DiffieHellman();
            var gc = local_keys.PublicKeyArray();

            using var accumulator = new MemoryStream();
            var sw_cleint_hello = Stopwatch.StartNew();
            await client_hello(gc, accumulator, ct);
            sw_cleint_hello.Stop();
            
            var sw_ap = Stopwatch.StartNew();
            var apResponseMessage = await get_ap_response(accumulator, ct);
            sw_ap.Stop();

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
            var n = new BigInteger(Consts.ServerKey, true, true)
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
                throw new AccessViolationException("Failed to verify APResponse");

            //ok to proceed
            var shared_secret = local_keys
                .ComputeSharedKey(remoteKey);

            var (challenge, send_key, recv_key) =
                compute_keys(shared_secret, accumulator);

            _sendCipher = new Shannon();
            _sendCipher.Key(send_key);

            _receiveCipher = new Shannon();
            _receiveCipher.Key(recv_key);

            sw_verify.Stop();

            var sw_client_response = Stopwatch.StartNew();
            await client_response(challenge, ct);
            _didHandshake = true;
            sw_client_response.Stop();
            sw_main.Stop();
            return true;
        }
    }

    public async Task<APWelcome> AuthenticateAsync(
        LoginCredentials loginCredentials,
        string deviceId,
        CancellationToken ct = default)
    {
        if (_isAuthenticated)
        {
            return _apWelcome;
        }

        using (await _authenticationLock.LockAsync(ct))
        {
            await HandshakeAsync(ct);

            static Os GetOperatingSystem()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Os.Osx;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return Os.Linux;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Os.Windows;
                }

                return Os.Linux;
            }

            
            var cpuFamily = CpuFamily.CpuX8664;
            var os = GetOperatingSystem();

            var packet = new ClientResponseEncrypted()
            {
                LoginCredentials = loginCredentials,
                SystemInfo = new SystemInfo
                {
                    CpuFamily = cpuFamily,
                    Os = os,
                    SystemInformationString = "eum-1-1",
                    DeviceId = deviceId
                },
                VersionString = "eum 1"
            };
            
            
            var loginPacket = new MercuryPacket(MercuryPacketType.Login, packet.ToByteArray());

            await SendPacketAsync(loginPacket, ct);

            var (cmd, payload) = await NextAsync(ct);
            _apWelcome = cmd switch
            {
                MercuryPacketType.APWelcome => APWelcome.Parser.ParseFrom(payload),
                MercuryPacketType.AuthFailure => throw new SpotifyAuthenticationException(
                    APLoginFailed.Parser.ParseFrom(payload)),
                _ => throw new ArgumentOutOfRangeException(nameof(cmd), $"Did not expect {cmd} packet with data: {System.Text.Encoding.UTF8.GetString(payload)}")
            };
            return _apWelcome;
        }
    }

    private readonly AsyncLock _sendLock = new AsyncLock();
    private readonly AsyncLock _receiveLock = new AsyncLock();

    private IShannon _receiveCipher;
    private IShannon _sendCipher;
    public async Task SendPacketAsync(MercuryPacket packet, CancellationToken ct = default)
    {
        var payload = packet.Payload;
        var cmd = packet.Cmd;
        using (await _sendLock.LockAsync(ct))
        {
            var payloadLengthAsByte = BitConverter.GetBytes((short)payload.Length).Reverse().ToArray();
            using var yetAnotherBuffer = new MemoryStream(3 + payload.Length);
            yetAnotherBuffer.WriteByte((byte)cmd);
            await yetAnotherBuffer.WriteAsync(payloadLengthAsByte, 0, payloadLengthAsByte.Length, ct);
            await yetAnotherBuffer.WriteAsync(payload, 0, payload.Length, ct);

            _sendCipher.DoNonce(_sendCipher.Nonce.ToByteArray());
            _sendCipher.Nonce += 1;

            var bufferBytes = yetAnotherBuffer.ToArray();
            _sendCipher.Encrypt(bufferBytes);

            var fourBytesBuffer = new byte[4];
            _sendCipher.Finish(fourBytesBuffer);

            var networkStream = tcpClient.GetStream();
            await networkStream.WriteAsync(bufferBytes, 0, bufferBytes.Length, ct);
            await networkStream.WriteAsync(fourBytesBuffer, 0, fourBytesBuffer.Length, ct);
            await networkStream.FlushAsync(ct);
        }
    }

    public async Task<MercuryPacket> NextAsync(CancellationToken ct = default)
    {
        using (await _receiveLock.LockAsync(ct))
        {
            _receiveCipher.DoNonce(_receiveCipher.Nonce
                .ToByteArray());
            _receiveCipher.Nonce += 1;
            //Make sure to re-set the nonce we just assigned.


            var headerBytes = new byte[3];
            var networkStream = tcpClient.GetStream();

            await networkStream.ReadCompleteAsync(headerBytes, 0,
                headerBytes.Length, ct);
            _receiveCipher.Decrypt(headerBytes);

            var cmd = headerBytes[0];
            var payloadLength = (short)((headerBytes[1] << 8) | (headerBytes[2] & 0xFF));

            var payloadBytes = new byte[payloadLength];
            await networkStream.ReadCompleteAsync(payloadBytes, 0, payloadBytes.Length, ct);
            _receiveCipher.Decrypt(payloadBytes);

            var mac = new byte[4];
            await networkStream.ReadCompleteAsync(mac, 0, mac.Length, ct);

            var expectedMac = new byte[4];
            _receiveCipher.Finish(expectedMac);
            return new MercuryPacket((MercuryPacketType)cmd, payloadBytes);
        }
    }

    private async Task client_response(byte[] challenge,
        CancellationToken ct =default)
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

        var networkStream = tcpClient.GetStream();
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
                    var payload = await networkStream.ReadCompleteAsync(ct);
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
            allkeys.copyOfRange(0x34, 0x54));
    }

    public bool IsAlive
    {
        get
        {
            return tcpClient is
            {
                Connected: true
            } && _didHandshake;
        }
    }

    
    /// <summary>
    /// Send the client-hello request to the sever, and returns the accumulated bytes on the response.
    /// This is needed to check the server signature.
    /// </summary>
    /// <param name="gc"></param>
    /// <param name="accumulator"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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

        var networkStream = tcpClient.GetStream();
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
        await accumulator.WriteAsync(lnarr, 0, lnarr.Length, ct);
        await accumulator.WriteAsync(buffer, 0, buffer.Length, ct);

    }

    private async Task<APResponseMessage> get_ap_response(MemoryStream accumulator,
        CancellationToken ct = default)
    {
        var networkStream = tcpClient.GetStream();
        
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

    private bool _didHandshake;

    private bool _isAuthenticated =>
        IsAlive && _apWelcome != null;

    private APWelcome _apWelcome;
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

public sealed class SpotifyAuthenticationException : Exception
{
    public SpotifyAuthenticationException(APLoginFailed parseFrom)
    {
        LoginFailed = parseFrom;
    }
    public APLoginFailed LoginFailed { get; }
}
