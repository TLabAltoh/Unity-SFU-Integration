using UnityEngine;

namespace TLab.SFU.Network
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "Room Config", menuName = "TLab/SFU/Room Config")]
    public class RoomConfig : ScriptableObject
    {
        [SerializeField] private string m_host = "127.0.0.1";

        [SerializeField] private uint m_port = 7777;

        [SerializeField] private string m_prefix = "http";

        [SerializeField] private Offer.CreateRoom m_init;

        public RoomConfig(string prefix, string host, uint port, Offer.CreateRoom init)
        {
            m_prefix = prefix;
            m_host = host;
            m_port = port;
            m_init = init;
        }

        public void GetAuth(out string roomKey, out string masterKey)
        {
            roomKey = m_init.room_key;
            masterKey = m_init.master_key;
        }

        public string GetPrefix() => m_prefix;

        public string GetHostPort() => m_host + ":" + m_port;

        public string GetHost() => m_host;

        public uint GetPort() => m_port;

        public string GetUrl() => m_prefix + "://" + GetHostPort();

        public Offer.CreateRoom GetCreateRoom() => m_init;

        public Offer.DeleteRoom GetDeleteRoom(int roomId, string masterKey)
        {
            return new Offer.DeleteRoom()
            {
                room_id = roomId,
                master_key = masterKey,
            };
        }

        public RoomConfig GetClone() => new RoomConfig(m_prefix, m_host, m_port, m_init);
    }
}
