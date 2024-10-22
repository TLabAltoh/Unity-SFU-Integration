using System.Collections;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Networked Object (TLab)")]
    [RequireComponent(typeof(NetworkedId))]
    public class NetworkedObject : MonoBehaviour
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        protected static string REGISTRY = "[registry] ";

        protected static void Register(Address64 id, NetworkedObject networkedObject) => m_registry.Add(id, networkedObject);

        protected static void UnRegister(Address64 id) => m_registry.Remove(id);

        public static void ClearRegistry()
        {
            var gameObjects = m_registry.Values.Cast<NetworkedObject>().Select((t) => t.gameObject);

            foreach (var gameObject in gameObjects)
                Destroy(gameObject);

            m_registry.Clear();
        }

        public static void Destroy(GameObject go)
        {
            if (go.GetComponent<NetworkedObject>() != null)
            {
                Destroy(go);
            }
        }

        public static void Destroy(Address64 id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
            {
                Destroy(go);
            }
        }

        public static NetworkedObject GetById(Address64 id) => m_registry[id] as NetworkedObject;

        #endregion REGISTRY

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

        public virtual void OnReceive(int to, int from, byte[] bytes) { }

        public virtual void SetSyncEnable(bool active)
        {
            m_enableSync = active;
        }

        public virtual void Shutdown()
        {
            if (m_state == State.SHUTDOWNED)
            {
                return;
            }

            m_enableSync = false;

            UnRegister(m_networkedId.id);

            m_state = State.SHUTDOWNED;
        }

        public virtual void Init(Address32 publicId)
        {
            if (m_state == State.INITIALIZED)
            {
                return;
            }

            m_networkedId = GetComponent<NetworkedId>();

            m_networkedId.SetPublicId(publicId);

            Register(m_networkedId.id, this);

            m_state = State.INITIALIZED;
        }

        public virtual void Init()
        {
            if (m_state == State.INITIALIZED)
            {
                return;
            }

            m_networkedId = GetComponent<NetworkedId>();

            Register(m_networkedId.id, this);

            m_state = State.INITIALIZED;
        }

        protected virtual void Awake() { }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void OnDestroy()
        {
            Shutdown();
        }

        protected virtual void OnApplicationQuit()
        {
            Shutdown();
        }
    }
}
