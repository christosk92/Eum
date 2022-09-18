using System.Runtime.InteropServices;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Contracts.CoreConnection;

namespace Eum.Cores.Spotify.Crypto;

public static class Integer
{
    /// <summary>
    ///     Left rotates an integer (bit shifting to the left with wrap) <br />
    ///     Bit rotating is defined as (A | B), with A = i &lt;&lt; distance and B = i &gt;&gt; distance
    /// </summary>
    /// <param name="i">The number to rotate.</param>
    /// <param name="distance">The amount of bits it should shift.</param>
    /// <returns></returns>
    public static int RotateLeft(this int i, int distance)
    {
        return (i << distance) | (int)((uint)i >> -distance);
    }

    /// <summary>
    ///     Right rotates an integer (bit shifting to the right with wrap) <br />
    ///     Bit rotating is defined as (A | B), with A = i &gt;&gt; distance and B = i &lt;&lt; distance
    /// </summary>
    /// <param name="i">The number to rotate.</param>
    /// <param name="distance">The amount of bits it should shift.</param>
    /// <returns></returns>
    public static int RotateRight(int i, int distance)
    {
        return (int)((uint)i >> distance) | (i << -distance);
    }
}

public class Shannon : object, IShannon
{
    private const int N = 16;
    private const int FOLD = 16;
    private const int INITKONST = 1771488570;
    private const int KEYP = 13;
    private readonly int[] CRC;
    private readonly int[] initR;
    private readonly int[] R;
    private int konst;
    private int mbuf;
    private int nbuf;
    private int sbuf;

    public Shannon()
    {
        R = new int[16];
        CRC = new int[16];
        initR = new int[16];
    }


    public int Nonce { get; set; }

    /// <inheritdoc />
    public void Key(byte[] key)
    {
        initState();
        loadKey(key);
        genKonst();
        saveState();
        nbuf = 0;
    }

    public void DoNonce(byte[] nonce)
    {
        reloadState();
        konst = 1771488570;
        loadKey(nonce);
        genKonst();
        nbuf = 0;
    }

    public void Encrypt(byte[] buffer)
    {
        encrypt(buffer, buffer.Length);
    }

    public void Finish(byte[] buffer)
    {
        finish(buffer, buffer.Length);
    }

    public void Decrypt(byte[] buffer)
    {
        decrypt(buffer, buffer.Length);
    }

    private int sbox([In] int obj0)
    {
        obj0 ^= obj0.RotateLeft(5) | obj0.RotateLeft(7);
        obj0 ^= obj0.RotateLeft(19) | obj0.RotateLeft(22);
        return obj0;
    }


    private int sbox2([In] int obj0)
    {
        obj0 ^= obj0.RotateLeft(7) | obj0.RotateLeft(22);
        obj0 ^= obj0.RotateLeft(5) | obj0.RotateLeft(19);
        return obj0;
    }

    private void crcFunc([In] int obj0)
    {
        var num = CRC[0] ^ CRC[2] ^ CRC[15] ^ obj0;
        for (var index = 1; index < 16; ++index)
            CRC[index - 1] = CRC[index];
        CRC[15] = num;
    }

    private void cycle()
    {
        var num1 = sbox(R[12] ^ R[13] ^ konst) ^ R[0].RotateLeft(1);
        for (var index = 1; index < 16; ++index)
            R[index - 1] = R[index];
        R[15] = num1;
        var num2 = sbox2(R[2] ^ R[15]);
        var r = R;
        var index1 = 0;
        var numArray = r;
        numArray[index1] = numArray[index1] ^ num2;
        sbuf = num2 ^ R[8] ^ R[12];
    }

    private void addKey([In] int obj0)
    {
        var r = R;
        var index = 13;
        var numArray = r;
        numArray[index] = numArray[index] ^ obj0;
    }

    private void diffuse()
    {
        for (var index = 0; index < 16; ++index)
            cycle();
    }

    private void initState()
    {
        R[0] = 1;
        R[1] = 1;
        for (var index = 2; index < 16; ++index)
            R[index] = R[index - 1] + R[index - 2];
        konst = 1771488570;
    }

    private void loadKey([In] byte[] obj0)
    {
        var numArray1 = new byte[4];
        int index1;
        for (index1 = 0; index1 < (obj0.Length & -4); index1 += 4)
        {
            addKey((obj0[index1 + 3] << 24) | (obj0[index1 + 2] << 16) | (obj0[index1 + 1] << 8) |
                   obj0[index1]);
            cycle();
        }

        if (index1 < obj0.Length)
        {
            var index2 = 0;
            for (; index1 < obj0.Length; ++index1)
            {
                var numArray2 = numArray1;
                var index3 = index2;
                ++index2;
                int num = (sbyte)obj0[index1];
                numArray2[index3] = (byte)num;
            }

            for (; index2 < 4; ++index2)
                numArray1[index2] = 0;
            addKey((numArray1[3] << 24) | (numArray1[2] << 16) | (numArray1[1] << 8) |
                   numArray1[0]);
            cycle();
        }

        addKey(obj0.Length);
        cycle();
        for (var index2 = 0; index2 < 16; ++index2)
            CRC[index2] = R[index2];
        diffuse();
        for (var index2 = 0; index2 < 16; ++index2)
        {
            var r = R;
            var index3 = index2;
            var numArray2 = r;
            numArray2[index3] = numArray2[index3] ^ CRC[index2];
        }
    }

    private void genKonst()
    {
        konst = R[0];
    }

    private void saveState()
    {
        for (var index = 0; index < 16; ++index)
            initR[index] = R[index];
    }

    private void reloadState()
    {
        for (var index = 0; index < 16; ++index)
            R[index] = initR[index];
    }

    private void macFunc([In] int obj0)
    {
        crcFunc(obj0);
        var r = R;
        var index = 13;
        var numArray = r;
        numArray[index] = numArray[index] ^ obj0;
    }

    public void encrypt(byte[] buffer, int n)
    {
        var index1 = 0;
        if (nbuf != 0)
        {
            for (; nbuf != 0 && n != 0; n += -1)
            {
                mbuf ^= buffer[index1] << (32 - nbuf);
                var numArray1 = buffer;
                var index2 = index1;
                var numArray2 = numArray1;
                numArray2[index2] = (byte)((sbyte)numArray2[index2] ^ ((sbuf >> (32 - nbuf)) & byte.MaxValue));
                ++index1;
                nbuf -= 8;
            }

            if (nbuf != 0)
                return;
            macFunc(mbuf);
        }

        for (var index2 = n & -4; index1 < index2; index1 += 4)
        {
            cycle();
            var num1 = (buffer[index1 + 3] << 24) | (buffer[index1 + 2] << 16) | (buffer[index1 + 1] << 8) |
                       buffer[index1];
            macFunc(num1);
            var num2 = num1 ^ sbuf;
            buffer[index1 + 3] = (byte)((num2 >> 24) & byte.MaxValue);
            buffer[index1 + 2] = (byte)((num2 >> 16) & byte.MaxValue);
            buffer[index1 + 1] = (byte)((num2 >> 8) & byte.MaxValue);
            buffer[index1] = (byte)(num2 & byte.MaxValue);
        }

        n &= 3;
        if (n == 0)
            return;
        cycle();
        mbuf = 0;
        for (nbuf = 32; nbuf != 0 && n != 0; n += -1)
        {
            mbuf ^= buffer[index1] << (32 - nbuf);
            var numArray1 = buffer;
            var index2 = index1;
            var numArray2 = numArray1;
            numArray2[index2] = (byte)((sbyte)numArray2[index2] ^ ((sbuf >> (32 - nbuf)) & byte.MaxValue));
            ++index1;
            nbuf -= 8;
        }
    }

    public void decrypt(byte[] buffer, int n)
    {
        var index1 = 0;
        if (nbuf != 0)
        {
            for (; nbuf != 0 && n != 0; n += -1)
            {
                var numArray1 = buffer;
                var index2 = index1;
                var numArray2 = numArray1;
                numArray2[index2] = (byte)((sbyte)numArray2[index2] ^ ((sbuf >> (32 - nbuf)) & byte.MaxValue));
                mbuf ^= buffer[index1] << (32 - nbuf);
                ++index1;
                nbuf -= 8;
            }

            if (nbuf != 0)
                return;
            macFunc(mbuf);
        }

        for (var index2 = n & -4; index1 < index2; index1 += 4)
        {
            cycle();
            var num = ((buffer[index1 + 3] << 24) | (buffer[index1 + 2] << 16) | (buffer[index1 + 1] << 8) |
                       buffer[index1]) ^ sbuf;
            macFunc(num);
            buffer[index1 + 3] = (byte)((num >> 24) & byte.MaxValue);
            buffer[index1 + 2] = (byte)((num >> 16) & byte.MaxValue);
            buffer[index1 + 1] = (byte)((num >> 8) & byte.MaxValue);
            buffer[index1] = (byte)(num & byte.MaxValue);
        }

        n &= 3;
        if (n == 0)
            return;
        cycle();
        mbuf = 0;
        for (nbuf = 32; nbuf != 0 && n != 0; n += -1)
        {
            var numArray1 = buffer;
            var index2 = index1;
            var numArray2 = numArray1;
            numArray2[index2] = (byte)((sbyte)numArray2[index2] ^ ((sbuf >> (32 - nbuf)) & byte.MaxValue));
            mbuf ^= buffer[index1] << (32 - nbuf);
            ++index1;
            nbuf -= 8;
        }
    }

    public void finish(byte[] buffer, int n)
    {
        var index1 = 0;
        if (nbuf != 0)
            macFunc(mbuf);
        cycle();
        addKey(1771488570 ^ (nbuf << 3));
        nbuf = 0;
        for (var index2 = 0; index2 < 16; ++index2)
        {
            var r = R;
            var index3 = index2;
            var numArray = r;
            numArray[index3] = numArray[index3] ^ CRC[index2];
        }

        diffuse();
        while (n > 0)
        {
            cycle();
            if (n >= 4)
            {
                buffer[index1 + 3] = (byte)((sbuf >> 24) & byte.MaxValue);
                buffer[index1 + 2] = (byte)((sbuf >> 16) & byte.MaxValue);
                buffer[index1 + 1] = (byte)((sbuf >> 8) & byte.MaxValue);
                buffer[index1] = (byte)(sbuf & byte.MaxValue);
                n += -4;
                index1 += 4;
            }
            else
            {
                for (var index2 = 0; index2 < n; ++index2)
                    buffer[index1 + index2] = (byte)((sbuf >> (index1 * 8)) & byte.MaxValue);
                break;
            }
        }
    }

    public void stream(byte[] buffer)
    {
        var num1 = 0;
        int length;
        for (length = buffer.Length; nbuf != 0 && length != 0; length += -1)
        {
            var numArray1 = buffer;
            var num2 = num1;
            ++num1;
            var index = num2;
            var numArray2 = numArray1;
            numArray2[index] = (byte)((sbyte)numArray2[index] ^ (sbuf & byte.MaxValue));
            sbuf >>= 8;
            nbuf -= 8;
        }

        for (var index1 = length & -4; num1 < index1; num1 += 4)
        {
            cycle();
            var numArray1 = buffer;
            var index2 = num1 + 3;
            var numArray2 = numArray1;
            numArray2[index2] = (byte)((sbyte)numArray2[index2] ^ ((sbuf >> 24) & byte.MaxValue));
            var numArray3 = buffer;
            var index3 = num1 + 2;
            var numArray4 = numArray3;
            numArray4[index3] = (byte)((sbyte)numArray4[index3] ^ ((sbuf >> 16) & byte.MaxValue));
            var numArray5 = buffer;
            var index4 = num1 + 1;
            var numArray6 = numArray5;
            numArray6[index4] = (byte)((sbyte)numArray6[index4] ^ ((sbuf >> 8) & byte.MaxValue));
            var numArray7 = buffer;
            var index5 = num1;
            var numArray8 = numArray7;
            numArray8[index5] = (byte)((sbyte)numArray8[index5] ^ (sbuf & byte.MaxValue));
        }

        var num3 = length & 3;
        if (num3 == 0)
            return;
        cycle();
        for (nbuf = 32; nbuf != 0 && num3 != 0; num3 += -1)
        {
            var numArray1 = buffer;
            var num2 = num1;
            ++num1;
            var index = num2;
            var numArray2 = numArray1;
            numArray2[index] = (byte)((sbyte)numArray2[index] ^ (sbuf & byte.MaxValue));
            sbuf >>= 8;
            nbuf -= 8;
        }
    }

    public void macOnly(byte[] buffer)
    {
        var index1 = 0;
        var length = buffer.Length;
        if (nbuf != 0)
        {
            for (; nbuf != 0 && length != 0; length += -1)
            {
                var shannon = this;
                var mbuf = shannon.mbuf;
                var numArray = buffer;
                var index2 = index1;
                ++index1;
                var num = (sbyte)numArray[index2] << (32 - nbuf);
                shannon.mbuf = mbuf ^ num;
                nbuf -= 8;
            }

            if (nbuf != 0)
                return;
            macFunc(this.mbuf);
        }

        for (var index2 = length & -4; index1 < index2; index1 += 4)
        {
            cycle();
            macFunc((buffer[index1 + 3] << 24) | (buffer[index1 + 2] << 16) | (buffer[index1 + 1] << 8) |
                    buffer[index1]);
        }

        var num1 = length & 3;
        if (num1 == 0)
            return;
        cycle();
        this.mbuf = 0;
        for (nbuf = 32; nbuf != 0 && num1 != 0; num1 += -1)
        {
            var shannon = this;
            var mbuf = shannon.mbuf;
            var numArray = buffer;
            var index2 = index1;
            ++index1;
            var num2 = (sbyte)numArray[index2] << (32 - nbuf);
            shannon.mbuf = mbuf ^ num2;
            nbuf -= 8;
        }
    }
}