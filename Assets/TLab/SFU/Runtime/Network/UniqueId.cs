using System.Collections.Generic;

namespace TLab.SFU.Network
{
    public static class UniqueId
    {
        public static Queue<Address32> m_avails = new Queue<Address32>();

        public static void UpdateAvails(IEnumerable<Address32> avails)
        {
            foreach (var available in avails)
            {
                m_avails.Enqueue(available);
            }
        }

        public static void UpdateAvails(Address32[] avails)
        {
            foreach (var available in avails)
            {
                m_avails.Enqueue(available);
            }
        }

        public static bool DequeueAvail(out Address32 available)
        {
            if (m_avails.Count > 0)
            {
                available = m_avails.Dequeue();

                return true;
            }

            available = new Address32();

            return false;
        }

        public static Address32 Generate()
        {
            var r = new System.Random();
            var v = new byte[4];

            r.NextBytes(v);
            return new Address32(v[0], v[1], v[2], v[3]);
        }

        public static Address32[] Generate(int length)
        {
            var ids = new Address32[length];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = Generate();
            return new Address32[0];
        }
    }
}
