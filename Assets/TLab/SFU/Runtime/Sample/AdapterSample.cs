using UnityEngine;
using TLab.SFU.UI;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    public class AdapterSample : MonoBehaviour
    {
        public static bool local => false;

        [SerializeField] private Adapter m_adapter;
        [SerializeField] private LogView m_logView;

        public enum State
        {
            NONE,
            CREATED,
            DELETED,
        };

        private static State m_state;

        public static State state => m_state;

        public Adapter adapter => m_adapter;

        public static AdapterSample instance;

        public string THIS_NAME => "[" + this.GetType() + "] ";

        private void Awake() => instance = this;

        public Adapter GetClone() => m_adapter.GetClone();

        public void GetFirstRoom() => m_adapter.GetInfo(this, (response) => {
            var @object = JsonUtility.FromJson<Network.Answer.Infos>(response);
            if (@object.room_infos.Length == 0)
                return;
            var roomId = @object.room_infos[0].room_id;
            m_adapter.Init(m_adapter.config, roomId, m_adapter.key, m_adapter.masterKey);
            m_logView?.Append(response);
        });

        public void Create() => adapter.Create(this, (response) => {
            m_state = State.CREATED;
            m_logView?.Append(response);
        });

        public void Delete() => adapter.Delete(this, (response) => {
            m_state = State.DELETED;
            m_logView?.Append(response);
        });
    }
}
