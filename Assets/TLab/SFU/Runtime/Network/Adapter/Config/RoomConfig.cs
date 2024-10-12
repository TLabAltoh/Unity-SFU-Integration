using UnityEngine;

namespace TLab.SFU.Network
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "Room Config", menuName = "TLab/SFU/Room Config")]
    public class RoomConfig : ScriptableObject
    {
        [System.Serializable]
        public class CreateOffer
        {
            public string room_name = "default";

            public uint room_capacity = 2;

            public string room_pass = "password";

            public bool needs_host = false;

            public bool is_public = false;

            public string master_key = "password";

            public string description = "description";
        }

        [System.Serializable]
        public class CreateAnswer
        {
            public int room_id;
            public string room_name;
            public uint room_capacity;
            public string description;
        };

        [System.Serializable]
        public class DeleteOffer
        {
            public int room_id;
            public string master_key;
        }

        [SerializeField] private string m_host = "127.0.0.1";

        [SerializeField] private uint m_port = 7777;

        [SerializeField] private string m_prefix = "http";

        [SerializeField] private CreateOffer m_createOffer;

        public RoomConfig(string prefix, string host, uint port, CreateOffer createOffer)
        {
            m_prefix = prefix;
            m_host = host;
            m_port = port;
            m_createOffer = createOffer;
        }

        public void GetAuth(out string room_pass, out string master_key)
        {
            room_pass = m_createOffer.room_pass;
            master_key = m_createOffer.master_key;
        }

        public string GetPrefix()
        {
            return m_prefix;
        }

        public string GetHostPort()
        {
            return m_host + ":" + m_port;
        }

        public string GetHost()
        {
            return m_host;
        }

        public uint GetPort()
        {
            return m_port;
        }

        public string GetUrl()
        {
            return m_prefix + "://" + GetHostPort();
        }

        public CreateOffer GetCreateOffer()
        {
            return m_createOffer;
        }

        public DeleteOffer GetDeleteOffer(int room_id)
        {
            return new DeleteOffer()
            {
                room_id = room_id,
                master_key = m_createOffer.master_key,
            };
        }

        public RoomConfig GetClone()
        {
            return new RoomConfig(m_prefix, m_host, m_port, m_createOffer);
        }
    }
}
