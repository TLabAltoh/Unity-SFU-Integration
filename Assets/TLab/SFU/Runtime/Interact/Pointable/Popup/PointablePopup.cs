using UnityEngine;

namespace TLab.SFU.Interact
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

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            var instance = m_popupController.GetFloatingAnchor(m_index);
            if (instance)
                instance.FadeInAsync();
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            var instance = m_popupController.GetFloatingAnchor(m_index);
            if (instance)
                instance.FadeOutAsync();
        }
    }
}
