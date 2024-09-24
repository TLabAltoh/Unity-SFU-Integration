using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.NetworkedVR
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
    }
}
