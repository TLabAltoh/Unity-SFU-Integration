using UnityEngine;

namespace TLab.SFU.Network
{
    using Registry = Registry<NetworkObject>;

    [AddComponentMenu("TLab/SFU/Network Object (TLab)")]
    [RequireComponent(typeof(NetworkId))]
    public class NetworkObject : MonoBehaviour
    {
        public enum State
        {
            NONE,
            INITIALIZED,
            SHUTDOWNED
        }

        [SerializeField] protected Direction m_direction = Direction.SENDRECV;

        protected State m_state = State.NONE;

        protected NetworkId m_networkId;

        protected bool m_synchronised = false;

        public State state => m_state;

        public NetworkId networkId => m_networkId;

        public bool synchronised => m_synchronised;

        public Direction direction { get => m_direction; set => m_direction = value; }

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public virtual void Shutdown()
        {
            if (m_state == State.SHUTDOWNED)
                return;

            m_direction = Direction.RECVONLY;

            if (m_networkId)
                UnRegister();

            m_state = State.SHUTDOWNED;
        }

        public virtual void Init(Address32 publicId)
        {
            if (m_state == State.INITIALIZED)
                return;

            m_networkId = GetComponent<NetworkId>();

            if (!m_networkId)
            {
                Debug.LogError(THIS_NAME + "NetworkId doesn't found");
                return;
            }

            m_networkId.SetPublicId(publicId);

            Register();

            m_state = State.INITIALIZED;

            Debug.Log(THIS_NAME + $"{nameof(Init)}:{gameObject.name}");
        }

        public virtual void Init()
        {
            if (m_state == State.INITIALIZED)
                return;

            m_networkId = GetComponent<NetworkId>();

            if (!m_networkId)
            {
                Debug.LogError(THIS_NAME + "NetworkId doesn't found");
                return;
            }

            Register();

            m_state = State.INITIALIZED;

            Debug.Log(THIS_NAME + $"{nameof(Init)}:{gameObject.name}");
        }

        public virtual void SyncViaWebRTC(bool force, int to) { }

        public virtual void SyncViaWebSocket(bool force, int to) { }

        protected virtual void Register() => Registry.Register(m_networkId.id, this);

        protected virtual void UnRegister() => Registry.UnRegister(m_networkId.id);

        protected virtual void Awake() { }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void OnEnable()
        {
            if ((m_state == State.INITIALIZED) && m_networkId)
                Register();
        }

        protected virtual void OnDisable()
        {
            if ((m_state == State.INITIALIZED) && m_networkId)
                UnRegister();
        }

        protected virtual void OnDestroy() => Shutdown();

        protected virtual void OnApplicationQuit() => Shutdown();
    }
}
