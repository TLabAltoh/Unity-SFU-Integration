using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU
{
    public static class ComponentExtention
    {
        public static void Foreach<T>(UnityAction<T> action) where T : MonoBehaviour
        {
            var @objects = Object.FindObjectsOfType<T>();
            foreach (var @object in @objects)
                action.Invoke(@object);
        }

        public static void Foreach<T>(this GameObject self, UnityAction<T> action) where T : MonoBehaviour
        {
            var @objects = self.GetComponentsInChildren<T>();
            foreach (var @object in @objects)
                action.Invoke(@object);
        }

        public static void Foreach<T>(this T[] @objects, UnityAction<T> action)
        {
            foreach (var @object in @objects)
                action.Invoke(@object);
        }

        public static T[] GetComponentsInTargets<T>(GameObject[] targets) where T : Component
        {
            var componentList = new List<T>();
            foreach (GameObject target in targets)
            {
                var component = target.GetComponent<T>();
                if (component != null)
                    componentList.Add(component);
            }
            return componentList.ToArray();
        }

        public static T RequireComponent<T>(this GameObject self) where T : Component
        {
            var result = self.GetComponent<T>();

            if (result == null)
                result = self.AddComponent<T>();

            return result;
        }

        public static void RemoveComponent<T>(this GameObject self) where T : Component
        {
            var result = self.GetComponent<T>();

            if (result != null)
                Object.Destroy(result);

            Object.Destroy(result);
        }
    }
}
