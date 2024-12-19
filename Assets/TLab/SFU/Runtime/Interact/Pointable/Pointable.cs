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

        public override void OnHover(Interactor interactor)
        {
            base.OnHover(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.Hover, interactor.pointer));
        }

        public override void WhileHover(Interactor interactor)
        {
            base.WhileHover(interactor);
        }

        public override void OnUnhover(Interactor interactor)
        {
            base.OnUnhover(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.UnHover, interactor.pointer));
        }

        public override void OnSelect(Interactor interactor)
        {
            base.OnSelect(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.Select, interactor.pointer));
        }

        public override void WhileSelect(Interactor interactor)
        {
            base.WhileSelect(interactor);

            whenPointerEventRaised?.Invoke(
                new PointerEvent(interactor.identifier, PointerEventType.Move, interactor.pointer));
        }

        public override void OnUnselect(Interactor interactor)
        {
            base.OnUnselect(interactor);

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

            Registry.Unregister(this);
        }
    }
}