using UnityEngine;
using TLab.VKeyborad;
using TLab.SFU.Network.Json;

namespace TLab.SFU.Network
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "Config", menuName = "TLab/SFU/Config")]
    public class Config : ScriptableObject, IInputHolder
    {
        [SerializeField] private string m_host = "127.0.0.1";

        [SerializeField] private uint m_port = 7777;

        [SerializeField] private string m_prefix = "http";

        [SerializeField] private string m_userName = "user";

        [SerializeField] private MediaConfig m_audio = new MediaConfig(true, false);

        [SerializeField] private CreateRoom.Request m_init;

        [System.Serializable]
        public class MediaConfig
        {
            public bool enable = true;
            public bool publishOnJoin = false;

            public MediaConfig() { }
            public MediaConfig(bool enable, bool publishOnJoin)
            {
                this.enable = enable;
                this.publishOnJoin = publishOnJoin;
            }
        }

        public Config(string prefix, string host, uint port, CreateRoom.Request init)
        {
            m_prefix = prefix;
            m_host = host;
            m_port = port;
            m_init = init;
        }

        public void GetAudio(out MediaConfig audio) => audio = m_audio;
        public void GetAudio(out bool enable, out bool publishOnJoin)
        {
            enable = m_audio.enable;
            publishOnJoin = m_audio.publishOnJoin;
        }
        public void SetAudio(MediaConfig audio) => m_audio = audio;

        public void GetAuth(out string sharedKey, out string masterKey)
        {
            sharedKey = m_init.sharedKey;
            masterKey = m_init.masterKey;
        }
        public void SetAuth(string sharedKey, string masterKey)
        {
            m_init.sharedKey = sharedKey;
            m_init.masterKey = masterKey;
        }

        public string GetPrefix() => m_prefix;
        public void SetPrefix(string prefix) => m_prefix = prefix;

        public string GetHostPort() => m_host + ":" + m_port;

        public string GetHost() => m_host;
        public void GetHost(string host) => m_host = host;

        public uint GetPort() => m_port;
        public void SetPort(uint port) => m_port = port;

        public string GetUrl() => m_prefix + "://" + GetHostPort();

        public string GetUserName() => m_userName;
        public void SetUserName(string userName) => m_userName = userName;

        public CreateRoom.Request GetInit() => m_init;

        public void SetInit(CreateRoom.Request init) => m_init = init;

        public Config GetClone() => new Config(m_prefix, m_host, m_port, m_init);

        public void OnValueChanged(string value) => m_host = value;

        public bool GetInitValue(out string value)
        {
            if (m_host == "")
            {
                value = "";
                return false;
            }

            value = m_host;
            return true;
        }
    }
}
