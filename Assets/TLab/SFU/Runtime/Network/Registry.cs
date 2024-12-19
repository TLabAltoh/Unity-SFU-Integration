using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace TLab.SFU
{
    public class Registry<K, V> where V : Component
    {
        private static Hashtable m_registry = new Hashtable();

        public static K[] keys => m_registry.Keys.Cast<K>().ToArray();
        public static V[] values => m_registry.Values.Cast<V>().ToArray();

        private static string THIS_NAME => "[" + nameof(Registry<K, V>) + "] ";

        public static void Register(K id, V @object)
        {
            if (!m_registry.ContainsKey(id)) m_registry[id] = @object;
        }

        public static void Unregister(K id)
        {
            if (m_registry.ContainsKey(id)) m_registry.Remove(id);
        }

        public static void ClearRegistry()
        {
            var gameObjects = m_registry.Values.Cast<V>().Select((t) => t.gameObject);

            foreach (var gameObject in gameObjects)
                Destroy(gameObject);

            m_registry.Clear();
        }

        public static void Destroy(GameObject go)
        {
            if (go.GetComponent<V>() != null)
                Destroy(go);
        }

        public static void Destroy(K id)
        {
            var go = GetByKey(id).gameObject;
            if (go != null)
                Destroy(go);
        }

        public static V GetByKey(K id) => m_registry[id] as V;
    }

    public class Registry<T> where T : Component
    {
        private static List<T> m_registry = new List<T>();

        public static List<T> registry => m_registry;

        public static string THIS_NAME = $"[{nameof(Registry<T>)}] ";

        public static void Register(T value)
        {
            if (!m_registry.Contains(value)) m_registry.Add(value);
        }

        public static void Unregister(T value)
        {
            if (m_registry.Contains(value)) m_registry.Remove(value);
        }
    }
}
