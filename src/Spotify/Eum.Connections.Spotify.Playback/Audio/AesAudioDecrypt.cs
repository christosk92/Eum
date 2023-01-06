using System;
using System.Diagnostics;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Eum.Connections.Spotify.Playback.Audio;

public class AesAudioDecrypt : IAudioDecrypt 
{
    public static readonly int CHUNK_SIZE = 2 * 128 * 1024;

    private static readonly byte[] AudioAesIv =
    {
        (byte) 0x72, (byte) 0xe0, (byte) 0x67, (byte) 0xfb, (byte) 0xdd, (byte) 0xcb, (byte) 0xcf, (byte) 0x77,
        (byte) 0xeb, (byte) 0xe8, (byte) 0xbc, (byte) 0x64, (byte) 0x3f, (byte) 0x63, (byte) 0x0d, (byte) 0x93
    };

    private static readonly BigInteger IvInt = new BigInteger(1, AudioAesIv);
    private static readonly BigInteger IvDiff = BigInteger.ValueOf(0x100);
    private int _decryptCount = 0;
    private long _decryptTotalTime = 0;
    private readonly IBufferedCipher _cipher;
    private readonly KeyParameter _spec;
    private int _decryptTimeMs;

    public AesAudioDecrypt(byte[] key)
    {
        _spec = ParameterUtilities.CreateKeyParameter("AES", key);
        _cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
    }

    public void DecryptChunk(int chunkIndex,byte[] buffer, int size = 0)
    {
        var iv = IvInt.Add(
            BigInteger.ValueOf(size == 0 ? CHUNK_SIZE * chunkIndex / 16
                : size * chunkIndex / 16));
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < buffer.Length; i += 4096)
        {
            _cipher.Init(true, new ParametersWithIV(_spec, iv.ToByteArray()));

            var count = Math.Min(4096, buffer.Length - i);
            var processed = _cipher.DoFinal(buffer,
                i,
                count,
                buffer, i);
            if (count != processed)
                throw new IOException(string.Format("Couldn't process all data, actual: %d, expected: %d",
                    processed, count));

            iv = iv.Add(IvDiff);
        }

        _decryptTotalTime += sw.ElapsedMilliseconds;
        _decryptCount++;
    }

    int IAudioDecrypt.DecryptTimeMs => _decryptTimeMs;

    /// <summary>
    /// Average decrypt time for <see cref="ChannelManager.CHUNK_SIZE"/> bytes of data.
    /// </summary>
    /// <returns></returns>
    public int DecryptTimeMs() =>
        _decryptCount == 0 ? 0 : (int)(((float)_decryptTotalTime / _decryptCount) / 1_000_000f);
}