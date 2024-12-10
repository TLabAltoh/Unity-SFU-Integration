using UnityEngine;
using TLab.SFU.UI;
using TLab.SFU.Network;
using TLab.SFU.Network.Json;

namespace TLab.SFU.Sample
{
    public class AdapterSample : MonoBehaviour
    {
        public static bool local => false;

        [SerializeField] private Adapter m_adapter;
        [SerializeField] private LogView m_logView;

        public enum State
        {
            None,
            Created,
            Deleted,
        };

        private static State m_state;

        public static State state => m_state;

        public Adapter adapter => m_adapter;

        public static AdapterSample instance;

        public string THIS_NAME => "[" + this.GetType() + "] ";

        private void Awake() => instance = this;

        public Adapter GetClone() => m_adapter.GetClone();

        public void GetFirstRoom() => m_adapter.GetInfo(this, (@string) => {
            var response = new RoomInfos(@string);

            if (response.infos.Length == 0)
                return;

            var roomId = response.infos[0].id;
            m_adapter.Init(m_adapter.config, roomId, m_adapter.sharedKey, m_adapter.masterKey);
            m_logView?.Append(@string);
        });

        private void OnCreate(string @string)
        {
            m_state = State.Created;
            m_logView?.Append(@string);
        }

        public void Create() => adapter.Create(this, OnCreate);

        private void OnDelete(string @string)
        {
            m_state = State.Deleted;
            m_logView?.Append(@string);
        }

        public void Delete() => adapter.Delete(this, OnDelete);
    }
}
