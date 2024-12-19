using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Scale Handle (TLab)")]
    public class ScaleHandle : GrabInteractable
    {
        private Interactor m_hand;

        private ScaleLogic m_logics;

        public Vector3 handPos => m_hand.pointer.position;

        public override void OnSelect(Interactor interactor)
        {
            base.OnSelect(interactor);

            if (m_hand == null)
            {
                m_hand = interactor;

                m_logics.HandleEnter(this);
            }
        }

        public override void OnUnselect(Interactor interactor)
        {
            base.OnUnselect(interactor);

            if (m_hand == interactor)
            {
                m_hand = null;

                m_logics.HandleExit(this);
            }
        }

        public override void WhileSelect(Interactor interactor)
        {
            base.WhileSelect(interactor);
        }

        public void RegistScalable(ScaleLogic logic)
        {
            if (m_logics == null)
            {
                m_logics = logic;
            }
        }

        public void UnRegistScalable(ScaleLogic logic)
        {
            if (m_logics == logic)
            {
                m_logics = null;
            }
        }
    }
}
