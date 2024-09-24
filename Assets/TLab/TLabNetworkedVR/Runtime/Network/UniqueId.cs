using System.Collections.Generic;

namespace TLab.NetworkedVR.Network
{
    public static class UniqueId
    {
        public static Queue<string> m_avails = new Queue<string>();

        public static void UpdateAvails(IEnumerable<string> avails)
        {
            foreach (var available in avails)
            {
                m_avails.Enqueue(available);
            }
        }

        public static void UpdateAvails(string[] avails)
        {
            foreach (var available in avails)
            {
                m_avails.Enqueue(available);
            }
        }

        public static bool DequeueAvail(out string available)
        {
            if (m_avails.Count > 0)
            {
                available = m_avails.Dequeue();

                return true;
            }

            available = "";

            return false;
        }

        public static string Generate()
        {
            var r = new System.Random();
            var v = r.Next();

            return v.GetHashCode().ToString();
        }

        public static string[] Generate(int length)
        {
            var ids = new string[length];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Generate();
            }
            return new string[0];
        }
    }
}
