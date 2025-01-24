using Unity.Mathematics;
using static System.BitConverter;

namespace TLab.SFU
{
    public static class UnsafeUtility
    {
        /// <summary>
        /// Quote from here: https://github.com/neuecc/MessagePack-CSharp/issues/117
        /// Fastest approach to copy buffers
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="count"></param>
        public static unsafe void LongCopy(byte* src, byte* dst, int count)
        {
            while (count >= 8)
            {
                *(ulong*)dst = *(ulong*)src;
                dst += 8;
                src += 8;
                count -= 8;
            }

            if (count >= 4)
            {
                *(uint*)dst = *(uint*)src;
                dst += 4;
                src += 4;
                count -= 4;
            }

            if (count >= 2)
            {
                *(ushort*)dst = *(ushort*)src;
                dst += 2;
                src += 2;
                count -= 2;
            }

            if (count >= 1)
            {
                *dst = *src;
            }
        }

        public unsafe static bool Get(byte* b) => *((bool*)&(b[0]));
        public unsafe static bool Get(byte[] buf, int offset)
        {
            fixed (byte* b = buf)
                return Get(b + offset);
        }

        public unsafe static void Copy(float[] src, byte* dst, int length, int startIndex = 0)
        {
            fixed (float* s = &(src[0]))
                LongCopy((byte*)s + startIndex, dst, length * sizeof(float));
        }

        public unsafe static void Copy(half[] src, byte* dst, int length, int startIndex = 0)
        {
            fixed (half* s = &(src[0]))
                LongCopy((byte*)s + startIndex, dst, length * sizeof(half));
        }

        public unsafe static void Copy(byte* src, float[] dst, int length, int startIndex = 0)
        {
            fixed (float* d = dst)
                LongCopy(src, (byte*)d + startIndex, length);
        }

        public unsafe static void Copy(byte* src, half[] dst, int length, int startIndex = 0)
        {
            fixed (half* d = dst)
                LongCopy(src, (byte*)d + startIndex, length);
        }

        public unsafe static void Copy(bool z, byte* dst) => dst[0] = *((byte*)(&z));

        public unsafe static void Copy(bool z, byte[] dst, int startIndex)
        {
            fixed (byte* d = dst)
                Copy(z, d + startIndex);
        }

        public unsafe static void Copy(int i, byte* dst)
        {
            var buf = GetBytes(i);
            fixed (byte* b = buf)
                LongCopy(b, dst, buf.Length);
        }

        public unsafe static void Copy(int i, byte[] dst, int startIndex = 0)
        {
            fixed (byte* d = dst)
                Copy(i, d + startIndex);
        }

        public unsafe static byte[] Padding(int length, byte[] bytes)
        {
            var padding = new byte[length + bytes.Length];
            fixed (byte* b = bytes, c = padding)
                LongCopy(b, c + length, bytes.Length);
            return padding;
        }

        public unsafe static byte[] Combine(byte[] bytes0, byte[] bytes1)
        {
            var padding = Padding(bytes0.Length, bytes1);
            fixed (byte* a = bytes0, c = padding)
                LongCopy(a, c, bytes0.Length);
            return padding;
        }

        public unsafe static byte[] Combine(int a, byte[] b) => Combine(GetBytes(a), b);
    }
}
