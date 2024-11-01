using UnityEngine;

namespace TLab.SFU.Network
{
    using Registry = Registry<NetworkedObject>;

    [AddComponentMenu("TLab/SFU/Networked Object (TLab)")]
    [RequireComponent(typeof(NetworkedId))]
    public class NetworkedObject : MonoBehaviour
    {
        public enum State
        {
            NONE,
            INITIALIZED,
            SHUTDOWNED
        }

        [Header("Sync Setting")]

        [SerializeField] protected bool m_enableSync = false;

        [SerializeField] protected string m_hash = "";

        protected State m_state = State.NONE;

        protected NetworkedId m_networkedId;

        protected bool m_syncFromOutside = false;

        public State state => m_state;

        public NetworkedId networkedId => m_networkedId;

        public bool syncFromOutside => m_syncFromOutside;

        public bool enableSync => m_enableSync;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public virtual void SetSyncEnable(bool active) => m_enableSync = active;

        public virtual void Shutdown()
        {
            if (m_state == State.SHUTDOWNED)
                return;

            m_enableSync = false;

            if (m_networkedId)
                Registry.UnRegister(m_networkedId.id);

            m_state = State.SHUTDOWNED;
        }

        public virtual void Init(Address32 publicId)
        {
            if (m_state == State.INITIALIZED)
                return;

            m_networkedId = GetComponent<NetworkedId>();

            if (!m_networkedId)
            {
                Debug.LogError(THIS_NAME + "NetworkedId doesn't found");
                return;
            }

            m_networkedId.SetPublicId(publicId);

            Registry.Register(m_networkedId.id, this);

            m_state = State.INITIALIZED;
        }

        public virtual void Init()
        {
            if (m_state == State.INITIALIZED)
                return;

            m_networkedId = GetComponent<NetworkedId>();

            if (!m_networkedId)
            {
                Debug.LogError(THIS_NAME + "NetworkedId doesn't found");
                return;
            }

            Registry.Register(m_networkedId.id, this);

            m_state = State.INITIALIZED;
        }

        public virtual void SyncViaWebRTC() { }

        public virtual void SyncViaWebSocket() { }

        protected virtual void Awake() { }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void OnDestroy() => Shutdown();

        protected virtual void OnApplicationQuit() => Shutdown();
    }
}
