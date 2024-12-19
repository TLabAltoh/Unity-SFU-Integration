using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Pointable Button (TLab)")]
    public class PointableButton : Pointable
    {
        [SerializeField] private UnityEvent[] m_onPress;

        [SerializeField] private UnityEvent[] m_onRelease;

        public override void OnSelect(Interactor interactor)
        {
            base.OnSelect(interactor);

            foreach (var callback in m_onPress)
                callback.Invoke();
        }

        public override void OnUnselect(Interactor interactor)
        {
            base.OnUnselect(interactor);

            foreach (var callback in m_onRelease)
                callback.Invoke();
        }
    }
}
