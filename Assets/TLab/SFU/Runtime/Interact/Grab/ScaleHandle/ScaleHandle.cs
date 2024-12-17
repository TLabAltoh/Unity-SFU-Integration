using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Scale Handle (TLab)")]
    public class ScaleHandle : GrabInteractable
    {
        private Interactor m_hand;

        private ScaleLogic m_logics;

        public Vector3 handPos => m_hand.pointer.position;

        public override void Selected(Interactor interactor)
        {
            base.Selected(interactor);

            if (m_hand == null)
            {
                m_hand = interactor;

                m_logics.HandleEnter(this);
            }
        }

        public override void UnSelected(Interactor interactor)
        {
            base.UnSelected(interactor);

            if (m_hand == interactor)
            {
                m_hand = null;

                m_logics.HandleExit(this);
            }
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);
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
