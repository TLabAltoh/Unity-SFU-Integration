using System.Collections;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/SimpleTracker (TLab)")]
    public class SimpleTracker : SyncTransformer
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static Hashtable registry => m_registry;

        protected static void Register(Address64 id, SimpleTracker tracker) => m_registry.Add(id, tracker);

        protected static new void UnRegister(Address64 id) => m_registry.Remove(id);

        public static new void ClearRegistry()
        {
            var gameObjects = m_registry.Values.Cast<SimpleTracker>().Select((t) => t.gameObject);

            foreach (var gameObject in gameObjects)
                Destroy(gameObject);

            m_registry.Clear();
        }

        public static void ClearObject(GameObject go)
        {
            if (go.GetComponent<SimpleTracker>() != null)
            {
                Destroy(go);
            }
        }

        public static void ClearObject(Address64 id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
            {
                ClearObject(go);
            }
        }

        public static new SimpleTracker GetById(Address64 id) => m_registry[id] as SimpleTracker;

        #endregion REGISTRY

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        private bool m_self = false;

        public bool self { get => m_self; set => m_self = value; }

        public override void Shutdown() => UnRegister(m_networkedId.id);

        protected override void Start()
        {
            base.Start();

            Register(m_networkedId.id, this);
        }

        protected override void Update()
        {
            base.Update();

            if (m_self)
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
