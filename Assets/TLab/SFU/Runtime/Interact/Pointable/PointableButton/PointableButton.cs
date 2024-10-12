using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Pointable Button (TLab)")]
    public class PointableButton : Pointable
    {
        [SerializeField] private UnityEvent[] m_onPress;

        [SerializeField] private UnityEvent[] m_onRelease;

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);
        }

        public override void Selected(Interactor interactor)
        {
            base.Selected(interactor);

            foreach (var callback in m_onPress)
            {
                callback.Invoke();
            }
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);
        }

        public override void UnSelected(Interactor interactor)
        {
            base.UnSelected(interactor);

            foreach (var callback in m_onRelease)
            {
                callback.Invoke();
            }
        }

        public override void WhileHovered(Interactor interactor)
        {
            base.WhileHovered(interactor);
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);
        }
    }
}
