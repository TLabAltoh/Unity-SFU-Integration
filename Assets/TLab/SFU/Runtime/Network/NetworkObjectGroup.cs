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

        private MSG_SyncRequest m_tmp = new MSG_SyncRequest(new Address64());

        private NetworkObject.State m_state = NetworkObject.State.None;

        public NetworkObject.State state => m_state;

        public NetworkId networkId => m_networkId;

        private Coroutine m_coroutine = null;

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
                    @object.OnGroupChanged(this);
                    m_registry.Add(@object);
                    UnityEditor.EditorUtility.SetDirty(@object);
                }
            }
        }
#endif

        private void RegisterOnMessage()
        {
            NetworkClient.RegisterOnMessage<MSG_SyncRequest>((from, to, bytes) =>
            {
                m_tmp.UnMarshall(bytes);

                Debug.Log(THIS_NAME + $"{nameof(MSG_SyncRequest)}:{gameObject.name}:{m_networkId.id.hash}");

                Registry.GetByKey(m_tmp.networkId)?.OnSyncRequested(from);
            });
        }

        private void OnSyncRequested(int from) => m_registry.Foreach((t) => t.OnSyncRequested(from));

        protected virtual void Register() => Registry.Register(m_networkId.id, this);

        protected virtual void UnRegister() => Registry.UnRegister(m_networkId.id);

        private void PostSyncRequest()
        {
            // Request synchronization
            m_tmp.networkId = m_networkId.id;
            NetworkClient.instance.SendWS(m_tmp.Marshall());
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

        public void InitAllObjects(Address32 publicId, bool self)
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

            if (!self)
                m_coroutine = StartCoroutine(WaitForInitialized());

            m_registry.Foreach((t) => t.Init(publicId, self));

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
                UnRegister();

            m_state = NetworkObject.State.Shutdowned;
        }

        private IEnumerator WaitForInitialized()
        {
            m_state = NetworkObject.State.Waiting0;

            var complete = false;
            while (!complete)
            {
                complete = true;

                m_registry.Foreach((t) => complete &= ((t.state == NetworkObject.State.Waiting1) || (t.state == NetworkObject.State.Initialized)));

                yield return new WaitForSeconds(1f);
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
                UnRegister();
        }

        private void OnDestroy() => Shutdown();

        private void OnApplicationQuit() => Shutdown();
    }
}
