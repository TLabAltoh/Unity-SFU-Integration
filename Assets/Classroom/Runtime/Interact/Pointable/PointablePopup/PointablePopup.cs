using UnityEngine;
using TLab.NetworkedVR.Interact;

namespace TLab.VRClassroom
{
    public class PointablePopup : PointableOutline
    {
        [SerializeField] private PopupController m_popupController;
        [SerializeField] private int m_index;

        public PopupController popupController
        {
            get
            {
                return m_popupController;
            }

            set
            {
                m_popupController = value;
            }
        }

        public int index { set => m_index = value; }

        protected override void Start()
        {
            base.Start();
        }

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            var instance = m_popupController.GetFloatingAnchor(m_index);
            if (instance)
            {
                instance.FadeIn();
            }
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            var instance = m_popupController.GetFloatingAnchor(m_index);
            if (instance)
            {
                instance.FadeOut();
            }
        }
    }
}
