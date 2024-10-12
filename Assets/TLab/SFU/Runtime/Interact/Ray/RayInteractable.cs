using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Ray Interactable (TLab)")]
    public class RayInteractable : Interactable
    {
        #region REGISTRY

        private static List<RayInteractable> m_registry = new List<RayInteractable>();

        public static new List<RayInteractable> registry => m_registry;

        public static void Register(RayInteractable handle)
        {
            if (!m_registry.Contains(handle))
            {
                m_registry.Add(handle);
            }
        }

        public static void UnRegister(RayInteractable handle)
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

            RayInteractable.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            RayInteractable.UnRegister(this);
        }
    }
}
