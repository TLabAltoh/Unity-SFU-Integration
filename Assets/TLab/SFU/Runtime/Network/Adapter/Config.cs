using UnityEngine;
using TLab.SFU.Network.Json;

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

        [SerializeField] private CreateRoom.Request m_init;

        public Config(string prefix, string host, uint port, CreateRoom.Request init)
        {
            m_prefix = prefix;
            m_host = host;
            m_port = port;
            m_init = init;
        }

        public void GetAuth(out string sharedKey, out string masterKey)
        {
            sharedKey = m_init.sharedKey;
            masterKey = m_init.masterKey;
        }

        public string GetPrefix() => m_prefix;

        public string GetHostPort() => m_host + ":" + m_port;

        public string GetHost() => m_host;

        public uint GetPort() => m_port;

        public string GetUrl() => m_prefix + "://" + GetHostPort();

        public string GetUserName() => m_userName;

        public CreateRoom.Request GetInit() => m_init;

        public Config GetClone() => new Config(m_prefix, m_host, m_port, m_init);
    }
}
