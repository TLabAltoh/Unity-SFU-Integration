using UnityEngine;

namespace TLab.SFU.Network
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "Config", menuName = "TLab/SFU/Config")]
    public class Config : ScriptableObject
    {
        [SerializeField] private string m_host = "127.0.0.1";

        [SerializeField] private uint m_port = 7777;

        [SerializeField] private string m_prefix = "http";

        [SerializeField] private string m_userName = "user";

        [SerializeField] private Offer.Create m_create;

        public Config(string prefix, string host, uint port, Offer.Create create)
        {
            m_prefix = prefix;
            m_host = host;
            m_port = port;
            m_create = create;
        }

        public void GetAuth(out string roomKey, out string masterKey)
        {
            roomKey = m_create.room_key;
            masterKey = m_create.master_key;
        }

        public string GetPrefix() => m_prefix;

        public string GetHostPort() => m_host + ":" + m_port;

        public string GetHost() => m_host;

        public uint GetPort() => m_port;

        public string GetUrl() => m_prefix + "://" + GetHostPort();

        public string GetUserName() => m_userName;

        public Offer.Create GetCreate() => m_create;

        public Config GetClone() => new Config(m_prefix, m_host, m_port, m_create);
    }
}