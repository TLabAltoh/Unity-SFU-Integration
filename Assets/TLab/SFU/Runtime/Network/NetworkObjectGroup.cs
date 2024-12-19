using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TLab.SFU.Network
{
    using Registry = Registry<Address64, NetworkObjectGroup>;

    [AddComponentMenu("TLab/SFU/Network Object Group (TLab)")]
    [RequireComponent(typeof(NetworkId))]
    public class NetworkObjectGroup : MonoBehaviour
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

        [SerializeField] private List<NetworkObject> m_registry;

        private NetworkId m_networkId;

        private static MSG_SyncRequest m_packet = new MSG_SyncRequest(new Address64());

        private NetworkObject.State m_state = NetworkObject.State.None;

        public NetworkObject.State state => m_state;

        public NetworkId networkId => m_networkId;

        private Coroutine m_coroutine = null;

        private int m_owner = -1;

        private Address32 m_public;

        private bool m_isPublicIdUsed = false;

        private static bool m_msgCallbackRegisted = false;

        public bool started => m_state != NetworkObject.State.None;

        public bool shutdowned => m_state == NetworkObject.State.Shutdowned;

        private string THIS_NAME => "[" + GetType() + "] ";

        public int length
        {
            get
            {
                if (m_registry == null)
                    return 0;
                return m_registry.Count;
            }
        }

        public int owner => m_owner;

        public Address32 @public => m_public;

#if UNITY_EDITOR
        public void UpdateRegistry()
        {
            m_registry = new List<NetworkObject>();

            var tmp = GetComponentsInChildren<NetworkObject>();

            foreach (var @object in tmp)
            {
                var group = GetComponentInParent<NetworkObjectGroup>(true);
                if (group == this)
                {
                    @object.OnGroupChange(this);
                    m_registry.Add(@object);
                    UnityEditor.EditorUtility.SetDirty(@object);
                }
            }
        }

        public void SetPrivateForAllChild()
        {
            var ids = GetComponentsInChildren<NetworkId>();
            var @privates = Address32.Generate(ids.Length);

            for (int i = 0; i < ids.Length; i++)
                ids[i].SetPrivate(privates[i]);
        }
#endif

        private void RegisterOnMessage()
        {
            NetworkClient.RegisterOnMessage<MSG_SyncRequest>((from, to, bytes) =>
            {
                m_packet.UnMarshall(bytes);

                var group = Registry.GetByKey(m_packet.networkId);
                if (group)
                    group.OnSyncRequest(from);
                else
                    Debug.LogWarning(THIS_NAME + $"group not found: {m_packet.networkId.hash}");
            });
        }

        private void OnSyncRequest(int from)
        {
            m_registry.Foreach((t) => t.OnSyncRequest(from));
            Debug.Log(THIS_NAME + $"{nameof(MSG_SyncRequest)}:{gameObject.name}:{m_packet.networkId.hash}");
        }

        protected virtual void Register() => Registry.Register(m_networkId.id, this);

        protected virtual void Unregister() => Registry.Unregister(m_networkId.id);

        public void PostSyncRequest()
        {
            // Request synchronization
            m_packet.networkId = m_networkId.id;
            NetworkClient.SendWS(m_packet.Marshall());
        }

        public void InitAllObjects(bool self)
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

            if (!self)
                m_coroutine = StartCoroutine(WaitForInitialized());

            m_registry.Foreach((t) => t.Init(self));

            if (!self)
                PostSyncRequest();
        }

        public void InitAllObjects(Address32 @public, int owner, bool self)
        {
            if (started)
                return;

            m_isPublicIdUsed = true;

            m_public.Copy(@public);

            m_owner = owner;

            m_networkId = GetComponent<NetworkId>();

            if (!m_networkId)
            {
                Debug.LogError(THIS_NAME + "NetworkId doesn't found");
                return;
            }

            m_networkId.SetPublic(@public);

            Register();

            if (!self)
                m_coroutine = StartCoroutine(WaitForInitialized());

            m_registry.Foreach((t) => t.Init(@public, self));

            if (!self)
                PostSyncRequest();
        }

        public void OnShutdown(NetworkObject @object)
        {
            if (m_registry.Contains(@object))
                m_registry.Remove(@object);
        }

        public virtual void Shutdown()
        {
            if (shutdowned)
                return;

            if (m_coroutine != null)
                StopCoroutine(m_coroutine);
            m_coroutine = null;

            if (m_networkId)
                Unregister();

            if (m_isPublicIdUsed)
                UniqueNetworkId.Return(m_public);

            m_state = NetworkObject.State.Shutdowned;
        }

        private IEnumerator WaitForInitialized()
        {
            m_state = NetworkObject.State.Waiting0;

            var complete = false;
            while (!complete)
            {
                yield return new WaitForSeconds(1f);

                complete = true;

                m_registry.Foreach((t) => complete &= ((t.state == NetworkObject.State.Waiting1) || (t.state == NetworkObject.State.Initialized)));
            }
            m_registry.Foreach((t) => t.NotifyInitComplete());

            m_state = NetworkObject.State.Initialized;

            m_coroutine = null;
        }

        private void Awake()
        {
            if (!m_msgCallbackRegisted)
            {
                RegisterOnMessage();
                m_msgCallbackRegisted = true;
            }
        }

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

        private void OnDestroy() => Shutdown();

        private void OnApplicationQuit() => Shutdown();
    }
}
