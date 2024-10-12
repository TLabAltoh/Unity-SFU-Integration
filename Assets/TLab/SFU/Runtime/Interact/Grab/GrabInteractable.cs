using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Grab Interactable (TLab)")]
    public class GrabInteractable : Interactable
    {
        #region REGISTRY

        private static List<GrabInteractable> m_registry = new List<GrabInteractable>();

        public static new List<GrabInteractable> registry => m_registry;

        public static void Register(GrabInteractable handle)
        {
            if (!m_registry.Contains(handle))
            {
                m_registry.Add(handle);
            }
        }

        public static void UnRegister(GrabInteractable handle)
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

            GrabInteractable.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            GrabInteractable.UnRegister(this);
        }
    }
}
