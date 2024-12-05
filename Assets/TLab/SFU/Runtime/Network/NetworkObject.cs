using UnityEngine;

namespace TLab.SFU.Network
{
    using Registry = Registry<Address64, NetworkObject>;

    [AddComponentMenu("TLab/SFU/Network Object (TLab)")]
    [RequireComponent(typeof(NetworkId))]
    public class NetworkObject : MonoBehaviour
    {
        [SerializeField, Message(typeof(MSG_SyncRequest))]
        public class MSG_SyncRequest : Message
        {
            public Address64 networkId;

            public MSG_SyncRequest(Address64 networkId)
            {
                this.networkId = networkId;
            }
        }

        public enum State
        {
            None,
            Waiting,
            Shutdowned,
            Initialized,
        }

        [SerializeField] protected Direction m_direction = Direction.SendRecv;

        protected State m_state = State.None;

        protected NetworkId m_networkId;

        protected bool m_synchronised = false;

        private MSG_SyncRequest m_tmp = new MSG_SyncRequest(new Address64());

        private static bool m_msgCallbackRegisted = false;

        public State state => m_state;

        public NetworkId networkId => m_networkId;

        public bool synchronised => m_synchronised;

        public Direction direction { get => m_direction; set => m_direction = value; }

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected virtual void BeforeShutdown() { }

        protected virtual void AfterShutdown() { }

        public virtual void Shutdown()
        {
            if (m_state == State.Shutdowned)
                return;

            BeforeShutdown();

            m_direction = Direction.RecvOnly;

            if (m_networkId)
                UnRegister();

            m_state = State.Shutdowned;

            AfterShutdown();
        }

        protected virtual void OnSyncRequestCompleted(int from) => m_state = (m_state == State.Waiting) ? State.Initialized : m_state;

        public virtual void Init(Address32 publicId, bool self)
        {
            if ((m_state == State.Initialized) || (m_state == State.Waiting))
                return;

            m_networkId = GetComponent<NetworkId>();

            if (!m_networkId)
            {
                Debug.LogError(THIS_NAME + "NetworkId doesn't found");
                return;
            }

            m_networkId.SetPublicId(publicId);

            Register();

            if (self)
                m_state = State.Initialized;
            else
            {
                m_state = State.Waiting;
                m_tmp.networkId = networkId.id;
                NetworkClient.instance.SendWS(m_tmp.Marshall());
            }

            Debug.Log(THIS_NAME + $"{nameof(Init)}:{gameObject.name}:{m_state}");
        }

        public virtual void Init(bool self)
        {
            if ((m_state == State.Initialized) || (m_state == State.Waiting))
                return;

            m_networkId = GetComponent<NetworkId>();

            if (!m_networkId)
            {
                Debug.LogError(THIS_NAME + "NetworkId doesn't found");
                return;
            }

            Register();

            if (self)
                m_state = State.Initialized;
            else
            {
                m_state = State.Waiting;
                m_tmp.networkId = networkId.id;
                NetworkClient.instance.SendWS(m_tmp.Marshall());
            }

            Debug.Log(THIS_NAME + $"{nameof(Init)}:{gameObject.name}:{m_state}");
        }

        public virtual void OnSyncRequested(int from) { }

        public virtual void SyncViaWebRTC(bool force, int to, bool requested = false) { }

        public virtual void SyncViaWebSocket(bool force, int to, bool requested = false) { }

        protected virtual void Register() => Registry.Register(m_networkId.id, this);

        protected virtual void UnRegister() => Registry.UnRegister(m_networkId.id);

        protected virtual void RegisterOnMessage()
        {
            NetworkClient.RegisterOnMessage<MSG_SyncRequest>((from, to, bytes) =>
            {
                m_tmp.UnMarshall(bytes);
                Registry.GetByKey(m_tmp.networkId)?.OnSyncRequested(from);
            });
        }

        protected virtual void Awake()
        {
            if (!m_msgCallbackRegisted)
            {
                RegisterOnMessage();
                m_msgCallbackRegisted = true;
            }
        }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void OnEnable()
        {
            if ((m_state == State.Initialized) && m_networkId)
                Register();
        }

        protected virtual void OnDisable()
        {
            if ((m_state == State.Initialized) && m_networkId)
                UnRegister();
        }

        protected virtual void OnDestroy() => Shutdown();

        protected virtual void OnApplicationQuit() => Shutdown();
    }
}
