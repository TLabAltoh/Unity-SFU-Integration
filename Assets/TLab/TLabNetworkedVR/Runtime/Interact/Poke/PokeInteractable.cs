using System.Collections.Generic;
using UnityEngine;

namespace TLab.NetworkedVR.Interact
{
    [AddComponentMenu("TLab/NetworkedVR/Poke Interactable (TLab)")]
    public class PokeInteractable : Interactable
    {
        #region REGISTRY

        private static List<PokeInteractable> m_registry = new List<PokeInteractable>();

        public static new List<PokeInteractable> registry => m_registry;

        public static void Register(PokeInteractable handle)
        {
            if (!m_registry.Contains(handle))
            {
                m_registry.Add(handle);
            }
        }

        public static void UnRegister(PokeInteractable handle)
        {
            if (m_registry.Contains(handle))
            {
                m_registry.Remove(handle);
            }
        }

        #endregion

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);
        }

        public override void WhileHovered(Interactor interactor)
        {
            base.WhileHovered(interactor);
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            PokeInteractable.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            PokeInteractable.UnRegister(this);
        }
    }
}
