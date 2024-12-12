using System.Collections.Generic;

namespace TLab.SFU.Network
{
    public static class UniqueNetworkId
    {
        private static Dictionary<int, HashSet<Address32>> m_historys = new Dictionary<int, HashSet<Address32>>();

        private static Queue<Address32> m_availables = new Queue<Address32>();

        public static void EnqueueAvailables(IEnumerable<Address32> availables)
        {
            foreach (var available in availables)
                m_availables.Enqueue(available);
        }

        public static void EnqueueAvailables(Address32[] availables)
        {
            foreach (var available in availables)
                m_availables.Enqueue(available);
        }

        public static bool DequeueAvailable(out Address32 available)
        {
            if (m_availables.Count > 0)
            {
                available = m_availables.Dequeue();

                return true;
            }

            available = new Address32();

            return false;
        }

        public static void OnUserExit(int userId) => m_historys.Remove(userId);

        public static Address32 Generate(int userId)
        {
            while (true)
            {
                var candidate = Address32.Generate();
                var history = m_historys.ContainsKey(userId) ? m_historys[userId] : new HashSet<Address32>();
                if (history.Contains(candidate))
                    continue;
                history.Add(candidate);
                m_historys[userId] = history;
                return candidate;
            }
        }

        public static Address32[] Generate(int userId, int length)
        {
            var ids = new Address32[length];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = Generate(userId);
            return ids;
        }
    }
}
