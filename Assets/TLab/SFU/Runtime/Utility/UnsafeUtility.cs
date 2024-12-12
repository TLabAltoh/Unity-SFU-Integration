using static System.BitConverter;

namespace TLab.SFU
{
    public static class UnsafeUtility
    {
        /// <summary>
        /// Quote from here: https://github.com/neuecc/MessagePack-CSharp/issues/117
        /// Fastest approach to copy buffers
        /// </summary>
        /// <param name="srcPtr"></param>
        /// <param name="dstPtr"></param>
        /// <param name="count"></param>
        public static unsafe void LongCopy(byte* srcPtr, byte* dstPtr, int count)
        {
            while (count >= 8)
            {
                *(ulong*)dstPtr = *(ulong*)srcPtr;
                dstPtr += 8;
                srcPtr += 8;
                count -= 8;
            }

            if (count >= 4)
            {
                *(uint*)dstPtr = *(uint*)srcPtr;
                dstPtr += 4;
                srcPtr += 4;
                count -= 4;
            }

            if (count >= 2)
            {
                *(ushort*)dstPtr = *(ushort*)srcPtr;
                dstPtr += 2;
                srcPtr += 2;
                count -= 2;
            }

            if (count >= 1)
            {
                *dstPtr = *srcPtr;
            }
        }

        public unsafe static bool Get(byte* bufPtr) => *((bool*)&(bufPtr[0]));
        public unsafe static bool Get(byte[] buf, int offset)
        {
            fixed (byte* bufPtr = buf)
                return Get(bufPtr + offset);
        }

        public unsafe static void Copy(float[] src, byte* dstPtr, int length, int startIndex = 0)
        {
            fixed (float* srcPtr = &(src[0]))
                LongCopy((byte*)srcPtr + startIndex, dstPtr, length * sizeof(float));
        }

        public unsafe static void Copy(byte* srcPtr, float[] dst, int length, int startIndex = 0)
        {
            fixed (float* dstPtr = dst)
                LongCopy(srcPtr, (byte*)dstPtr + startIndex, length);
        }

        public unsafe static void Copy(bool z, byte* dstPtr) => dstPtr[0] = *((byte*)(&z));

        public unsafe static void Copy(bool z, byte[] dst, int startIndex)
        {
            fixed (byte* dstPtr = dst)
                Copy(z, dstPtr + startIndex);
        }

        public unsafe static void Copy(int i, byte* dstPtr)
        {
            var buf = GetBytes(i);
            fixed (byte* bufPtr = buf)
                LongCopy(bufPtr, dstPtr, buf.Length);
        }

        public unsafe static void Copy(int i, byte[] dst, int startIndex = 0)
        {
            fixed (byte* dstPtr = dst)
                Copy(i, dstPtr + startIndex);
        }

        public unsafe static byte[] Padding(int length, byte[] b)
        {
            var c = new byte[length + b.Length];
            fixed (byte* bPtr = b, cPtr = c)
                LongCopy(bPtr, cPtr + length, b.Length);
            return c;
        }

        public unsafe static byte[] Combine(byte[] a, byte[] b)
        {
            var c = Padding(a.Length, b);
            fixed (byte* aPtr = a, cPtr = c)
                LongCopy(aPtr, cPtr, a.Length);
            return c;
        }

        public unsafe static byte[] Combine(int a, byte[] b) => Combine(GetBytes(a), b);
    }
}
