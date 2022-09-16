namespace Eum.Cores.Spotify.Helpers
{
    internal static class BitShiftHelpers
    {

        #region PRivates

        internal static short getShort(this byte[] obj0, int obj1, bool obj2)
        {
            return (short)(!obj2 ? (int)getShortL(obj0, obj1) : (int)getShortB(obj0, obj1));
        }

        internal static short getShortB(this byte[] obj0, int obj1)
        {
            return makeShort(obj0[obj1], obj0[obj1 + 1]);
        }

        internal static short getShortL(this byte[] obj0, int obj1)
        {
            return makeShort(obj0[obj1 + 1], obj0[obj1]);
        }

        internal static short makeShort(this byte obj0, byte obj1)
        {
            return (short)(((sbyte)obj0 << 8) | ((sbyte)obj1 & byte.MaxValue));
        }

        internal static int getInt(this byte[] obj0, int obj1, bool obj2)
        {
            return !obj2 ? getIntL(obj0, obj1) : getIntB(obj0, obj1);
        }

        internal static int getIntB(this byte[] obj0, int obj1)
        {
            return makeInt(obj0[obj1], obj0[obj1 + 1], obj0[obj1 + 2], obj0[obj1 + 3]);
        }

        internal static int getIntL(this byte[] obj0, int obj1)
        {
            return makeInt(obj0[obj1 + 3], obj0[obj1 + 2], obj0[obj1 + 1], obj0[obj1]);
        }

        internal static int makeInt(this byte obj0, byte obj1, byte obj2, byte obj3)
        {
            return ((sbyte)obj0 << 24) | (((sbyte)obj1 & byte.MaxValue) << 16) |
                   (((sbyte)obj2 & byte.MaxValue) << 8) | ((sbyte)obj3 & byte.MaxValue);
        }

        internal static long getLong(this byte[] obj0, int obj1, bool obj2)
        {
            return !obj2 ? getLongL(obj0, obj1) : getLongB(obj0, obj1);
        }

        internal static long getLongB(this byte[] obj0, int obj1)
        {
            return makeLong(obj0[obj1], obj0[obj1 + 1], obj0[obj1 + 2], obj0[obj1 + 3], obj0[obj1 + 4], obj0[obj1 + 5],
                obj0[obj1 + 6], obj0[obj1 + 7]);
        }

        internal static long getLongL(this byte[] obj0, int obj1)
        {
            return makeLong(obj0[obj1 + 7], obj0[obj1 + 6], obj0[obj1 + 5], obj0[obj1 + 4], obj0[obj1 + 3],
                obj0[obj1 + 2], obj0[obj1 + 1], obj0[obj1]);
        }

        internal static long makeLong(this
            byte obj0,
            byte obj1,
            byte obj2,
            byte obj3,
            byte obj4,
            byte obj5,
            byte obj6,
            byte obj7)
        {
            return ((long)(sbyte)obj0 << 56)
                   | (((sbyte)obj1 & (long)byte.MaxValue) << 48)
                   | (((sbyte)obj2 & (long)byte.MaxValue) << 40)
                   | (((sbyte)obj3 & (long)byte.MaxValue) << 32)
                   | (((sbyte)obj4 & (long)byte.MaxValue) << 24)
                   | (((sbyte)obj5 & (long)byte.MaxValue) << 16)
                   | (((sbyte)obj6 & (long)byte.MaxValue) << 8)
                   | ((sbyte)obj7 & (long)byte.MaxValue);
        }

        #endregion
    }
}