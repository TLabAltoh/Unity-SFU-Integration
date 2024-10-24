using System.Collections.Generic;

namespace TLab.SFU.Interact
{
    public class Registry<T> : Interactable
    {
        private static List<Interactable> m_registry = new List<Interactable>();

        public static List<Interactable> registry => m_registry;

        public static string THIS_NAME = "[registry] ";

        public static void Register(Interactable interactable)
        {
            if (!m_registry.Contains(interactable)) m_registry.Add(interactable);
        }

        public static void UnRegister(Interactable interactable)
        {
            if (m_registry.Contains(interactable)) m_registry.Remove(interactable);
        }
    }
}
