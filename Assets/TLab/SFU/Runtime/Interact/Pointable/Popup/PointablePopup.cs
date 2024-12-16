using UnityEngine;

namespace TLab.SFU.Interact
{
    public class PointablePopup : PointableOutline
    {
        [Header("Popup")]
        [SerializeField] private int m_index;
        [SerializeField] private PopupController m_controller;

        public PopupController controller
        {
            get => m_controller;
            set
            {
                if (m_controller != value)
                {
                    m_controller = value;
                }
            }
        }

        public int index { set => m_index = value; }

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            var instance = m_controller.GetFloatingAnchor(m_index);
            if (instance)
                instance.FadeInAsync();
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            var instance = m_controller.GetFloatingAnchor(m_index);
            if (instance)
                instance.FadeOutAsync();
        }
    }
}
