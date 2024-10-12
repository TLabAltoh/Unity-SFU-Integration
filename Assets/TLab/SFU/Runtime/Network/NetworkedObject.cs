using System.Collections.Generic;
using System.Collections;
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

        protected static void Register(string id, NetworkedObject networkedObject)
        {
            if (!m_registry.ContainsKey(id))
            {
                m_registry[id] = networkedObject;
            }
        }

        protected static void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
            {
                m_registry.Remove(id);
            }
        }

        public static void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var networkedObject = entry.Value as NetworkedObject;
                gameobjects.Add(networkedObject.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static void Destroy(GameObject go)
        {
            if (go.GetComponent<NetworkedObject>() != null)
            {
                Destroy(go);
            }
        }

        public static void Destroy(string id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
            {
                Destroy(go);
            }
        }

        public static NetworkedObject GetById(string id) => m_registry[id] as NetworkedObject;

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

        protected static unsafe void LongCopy(byte* src, byte* dst, int count)
        {
            // https://github.com/neuecc/MessagePack-CSharp/issues/117

            while (count >= 8)
            {
                *(ulong*)dst = *(ulong*)src;
                dst += 8;
                src += 8;
                count -= 8;
            }

            if (count >= 4)
            {
                *(uint*)dst = *(uint*)src;
                dst += 4;
                src += 4;
                count -= 4;
            }

            if (count >= 2)
            {
                *(ushort*)dst = *(ushort*)src;
                dst += 2;
                src += 2;
                count -= 2;
            }

            if (count >= 1)
            {
                *dst = *src;
            }
        }

        public virtual void OnRTCMessage(string dst, string src, byte[] bytes)
        {

        }

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

        public virtual void Init(string id)
        {
            if (m_state == State.INITIALIZED)
            {
                return;
            }

            m_networkedId = GetComponent<NetworkedId>();

            m_networkedId.SetPublicId(id);

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

        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {

        }

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
