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

            public MSG_SyncRequest(Address64 networkId) : base()
            {
                this.networkId = networkId;
            }

            public MSG_SyncRequest(byte[] bytes) : base(bytes) { }
        }

        [SerializeField, Message(typeof(MSG_Sync))]
        public class MSG_Sync : Message
        {
            public bool requested = false;

            public MSG_Sync(bool requested = false) : base()
            {
                this.requested = requested;
            }

            public MSG_Sync(byte[] bytes) : base(bytes) { }
        }

        public enum State
        {
            None,
            Waiting0,   // Wait for first synchronization
            Waiting1,   // Wait for initialize complete
            Shutdowned,
            Initialized,
        }

        [SerializeField] protected Direction m_direction = Direction.SendRecv;
        [SerializeField] protected NetworkObjectGroup m_group;

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
            if (shutdowned)
                return;

            BeforeShutdown();

            m_direction = Direction.RecvOnly;

            if (m_networkId)
                UnRegister();

            m_state = State.Shutdowned;

            m_group.OnShutdown(this);

            AfterShutdown();
        }

#if UNITY_EDITOR
        public virtual void OnGroupChanged(NetworkObjectGroup group)
        {
            m_group = group;
        }
#endif

        protected virtual void OnSyncRequestCompleted(int from)
        {
            m_state = (m_state == State.Waiting0) ? State.Waiting1 : m_state;
            Debug.Log(THIS_NAME + $"{nameof(OnSyncRequestCompleted)}:{gameObject.name}");
        }

        public bool started => m_state != State.None;

        public bool shutdowned => m_state == State.Shutdowned;

        protected virtual void OnInitComplete() { }

        public virtual void NotifyInitComplete()
        {
            if (m_state == State.Initialized)
                return;

            m_state = State.Initialized;

            OnInitComplete();
        }

        protected virtual void Move2Waiting0()
        {
            m_state = State.Waiting0;
        }

        public virtual void Init(Address32 publicId, bool self)
        {
            if (started)
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
                NotifyInitComplete();
            else
                Move2Waiting0();
        }

        public virtual void Init(bool self)
        {
            if (started)
                return;

            m_networkId = GetComponent<NetworkId>();

            if (!m_networkId)
            {
                Debug.LogError(THIS_NAME + "NetworkId doesn't found");
                return;
            }

            Register();

            if (self)
                NotifyInitComplete();
            else
                Move2Waiting0();
        }

        public virtual void OnSyncRequested(int from)
        {
            Debug.Log(THIS_NAME + $"{nameof(OnSyncRequested)}:{gameObject.name}");
        }

        public virtual void SyncViaWebRTC(int to, bool force = false, bool requested = false) { }

        public virtual void SyncViaWebSocket(int to, bool force = false, bool requested = false) { }

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
            if (started && m_networkId)
                Register();
        }

        protected virtual void OnDisable()
        {
            if (started && m_networkId)
                UnRegister();
        }

        protected virtual void OnDestroy() => Shutdown();

        protected virtual void OnApplicationQuit() => Shutdown();
    }
}
