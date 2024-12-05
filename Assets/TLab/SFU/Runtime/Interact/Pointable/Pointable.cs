using System;
using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<Pointable>;

    public enum PointerEventType
    {
        Hover,
        UnHover,
        Select,
        UnSelect,
        Move,
        Cancel,
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
        public event Action<PointerEvent> whenPointerEventRaised;

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.Hover, interactor.pointer));
        }

        public override void WhileHovered(Interactor interactor)
        {
            base.WhileHovered(interactor);
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.UnHover, interactor.pointer));
        }

        public override void Selected(Interactor interactor)
        {
            base.Selected(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.Select, interactor.pointer));
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.Move, interactor.pointer));
        }

        public override void UnSelected(Interactor interactor)
        {
            base.UnSelected(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.UnSelect, interactor.pointer));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Registry.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Registry.UnRegister(this);
        }
    }
}