using System.Collections;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    public class Registry<T> where T : Component
    {
        private static Hashtable m_registry = new Hashtable();

        private static string THIS_NAME => "[" + nameof(Registry<T>) + "] ";

        public static void Register(Address64 id, T @object)
        {
            if (!m_registry.ContainsKey(id)) m_registry[id] = @object;
        }

        public static void UnRegister(Address64 id)
        {
            if (m_registry.ContainsKey(id)) m_registry.Remove(id);
        }

        public static void ClearRegistry()
        {
            var gameObjects = m_registry.Values.Cast<T>().Select((t) => t.gameObject);

            foreach (var gameObject in gameObjects)
                Destroy(gameObject);

            m_registry.Clear();
        }

        public static void Destroy(GameObject go)
        {
            if (go.GetComponent<T>() != null)
                Destroy(go);
        }

        public static void Destroy(Address64 id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
                Destroy(go);
        }

        public static T GetById(Address64 id) => m_registry[id] as T;
    }
}
