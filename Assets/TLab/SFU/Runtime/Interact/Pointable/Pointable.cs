using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU.Interact
{
    public enum PointerEventType
    {
        HOVER,
        UNHOVER,
        SELECT,
        UNSELECT,
        MOVE,
        CANCEL
    }

    public struct PointerEvent
    {
        public int identifier { get; }
        public PointerEventType type { get; }
        public Transform pointer { get; }

        public PointerEvent(int identifier, PointerEventType type, Transform pointer)
        {
            this.identifier = identifier;
            this.type = type;
            this.pointer = pointer;
        }
    }

    [AddComponentMenu("TLab/SFU/" + nameof(Pointable) + " (TLab)")]
    public class Pointable : Interactable
    {
        #region REGISTRY

        private static List<Pointable> m_registry = new List<Pointable>();

        public static new List<Pointable> registry => m_registry;

        public static void Register(Pointable pointable)
        {
            if (!m_registry.Contains(pointable))
            {
                m_registry.Add(pointable);
            }
        }

        public static void UnRegister(Pointable pointable)
        {
            if (m_registry.Contains(pointable))
            {
                m_registry.Remove(pointable);
            }
        }

        #endregion

        public event Action<PointerEvent> whenPointerEventRaised;

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.HOVER, interactor.pointer));
        }

        public override void WhileHovered(Interactor interactor)
        {
            base.WhileHovered(interactor);
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.UNHOVER, interactor.pointer));
        }

        public override void Selected(Interactor interactor)
        {
            base.Selected(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.SELECT, interactor.pointer));
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.MOVE, interactor.pointer));
        }

        public override void UnSelected(Interactor interactor)
        {
            base.UnSelected(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.UNSELECT, interactor.pointer));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Pointable.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Pointable.UnRegister(this);
        }
    }
}