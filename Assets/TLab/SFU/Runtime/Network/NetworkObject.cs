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
            public bool request = false;
            public bool immediate = false;

            public MSG_Sync(bool request = false, bool immediate = false) : base()
            {
                this.request = request;
                this.immediate = immediate;
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

        public enum SocketType
        {
            WebRTC,
            WebSocket,
        }

        [SerializeField] protected Direction m_direction = Direction.SendRecv;
        [SerializeField] protected SocketType m_syncDefault = SocketType.WebRTC;
        [SerializeField] protected NetworkObjectGroup m_group;

        private delegate void SyncFunc(int to, bool force = false, bool request = false, bool immediate = false);

        private SyncFunc m_funcSyncDefault;

        protected State m_state = State.None;

        protected NetworkId m_networkId;

        protected bool m_synchronised = false;

        private static MSG_SyncRequest m_packet = new MSG_SyncRequest(new Address64());

        public State state => m_state;

        public NetworkId networkId => m_networkId;

        public SocketType syncDefault => m_syncDefault;

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
                Unregister();

            m_state = State.Shutdowned;

            m_group.OnShutdown(this);

            AfterShutdown();
        }

#if UNITY_EDITOR
        public virtual void OnGroupChange(NetworkObjectGroup group)
        {
            m_group = group;
        }
#endif

        protected virtual void OnSyncRequestComplete(int from)
        {
            m_state = (m_state == State.Waiting0) ? State.Waiting1 : m_state;
        }

        public bool started => m_state != State.None;

        public bool shutdowned => m_state == State.Shutdowned;

        public bool initialized => m_state == State.Initialized;

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

        public virtual void Init(in Address32 @public, bool self)
        {
            if (started)
                return;

            m_networkId = GetComponent<NetworkId>();

            if (!m_networkId)
            {
                Debug.LogError(THIS_NAME + "NetworkId doesn't found");
                return;
            }

            m_networkId.SetPublic(@public);

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

        public virtual void SyncViaWebRTC(int to, bool force = false, bool request = false, bool immediate = false) { }

        public virtual void SyncViaWebSocket(int to, bool force = false, bool request = false, bool immediate = false) { }

        public virtual void Sync(int to, bool force = false, bool request = false, bool immediate = false) => m_funcSyncDefault.Invoke(to, force, request, immediate);

        public virtual void OnSyncRequest(int from) { }

        protected virtual void Register()
        {
            RegisterOnMessage();
            Registry.Register(m_networkId.id, this);
        }

        protected virtual void Unregister() => Registry.Unregister(m_networkId.id);

        protected virtual void RegisterOnMessage()
        {
            NetworkClient.RegisterOnMessage<MSG_SyncRequest>((from, to, bytes) =>
            {
                m_packet.UnMarshall(bytes);
                Registry.GetByKey(m_packet.networkId)?.OnSyncRequest(from);
            });
        }

        protected virtual void Start()
        {
            switch (m_syncDefault)
            {
                case SocketType.WebRTC:
                    m_funcSyncDefault = SyncViaWebRTC;
                    break;
                case SocketType.WebSocket:
                    m_funcSyncDefault = SyncViaWebSocket;
                    break;
            }
        }

        protected virtual void Update() { }

        protected virtual void OnEnable()
        {
            if (started && m_networkId)
                Register();
        }

        protected virtual void OnDisable()
        {
            if (started && m_networkId)
                Unregister();
        }

        protected virtual void OnDestroy() => Shutdown();

        protected virtual void OnApplicationQuit() => Shutdown();
    }
}
