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

        public override void OnHover(Interactor interactor)
        {
            base.OnHover(interactor);

            var instance = m_controller.GetFloatingAnchor(m_index);
            if (instance)
                instance.Fade(1);
        }

        public override void OnUnhover(Interactor interactor)
        {
            base.OnUnhover(interactor);

            var instance = m_controller.GetFloatingAnchor(m_index);
            if (instance)
                instance.Fade(0);
        }
    }
}
