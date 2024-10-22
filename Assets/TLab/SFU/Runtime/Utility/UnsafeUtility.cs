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

        public unsafe static byte[] Combine(byte[] a, byte[] b)
        {
            var c = new byte[a.Length + b.Length];
            fixed (byte* aPtr = a, bPtr = b, cPtr = c)
            {
                LongCopy(aPtr, cPtr, a.Length);
                LongCopy(bPtr, cPtr + a.Length, b.Length);
            }
            return c;
        }

        public unsafe static byte[] Combine(int a, byte[] b) => Combine(System.BitConverter.GetBytes(a), b);
    }
}
