using System.Collections;
using System.Linq;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU.Humanoid
{
    [AddComponentMenu("TLab/SFU/Body Tracker (TLab)")]
    public class BodyTracker : SyncTransformer
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static Hashtable registry => m_registry;

        protected static void Register(Address64 id, BodyTracker tracker) => m_registry.Add(id, tracker);

        protected static new void UnRegister(Address64 id) => m_registry.Remove(id);

        public static new void ClearRegistry()
        {
            var gameObjects = m_registry.Values.Cast<BodyTracker>().Select((t) => t.gameObject);

            foreach (var gameObject in gameObjects)
                Destroy(gameObject);

            m_registry.Clear();
        }

        public static new void Destroy(GameObject go)
        {
            if (go.GetComponent<BodyTracker>() != null)
            {
                Destroy(go);
            }
        }

        public static new void Destroy(Address64 id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
            {
                Destroy(go);
            }
        }

        public static new BodyTracker GetById(Address64 id) => m_registry[id] as BodyTracker;

        #endregion REGISTRY

        [System.Serializable]
        public class TrackTarget
        {
            public AvatorConfig.PartsType parts;

            public Transform target;
        }

        [SerializeField] private AvatorConfig.PartsType m_partsType;

        [SerializeField] private bool m_self = false;

        [SerializeField] private bool m_initializeOnStartUp = false;

        private bool m_initialized = false;

        public void Destroy()
        {
            UnRegister(m_networkedId.id);
        }

        public void Init(AvatorConfig.PartsType partsType, bool self)
        {
            if (m_initialized)
            {
                Debug.LogError(THIS_NAME + "This tracker has already been initialised.");

                return;
            }

            m_partsType = partsType;

            m_self = self;

            m_initialized = true;
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            if (m_initializeOnStartUp)
            {
                Init(m_partsType, m_self);
            }

            base.Start();

            Register(m_networkedId.id, this);
        }

        protected override void Update()
        {
            base.Update();

            if (m_initialized && m_self)
            {
                SyncTransformViaWebRTC();
            }
        }

        protected override void OnApplicationQuit()
        {
            Shutdown();

            base.OnApplicationQuit();
        }

        protected override void OnDestroy()
        {
            Shutdown();

            base.OnDestroy();
        }
    }
}
