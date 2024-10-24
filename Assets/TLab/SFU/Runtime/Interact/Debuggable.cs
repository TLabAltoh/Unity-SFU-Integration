using UnityEngine;
using TMPro;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Debugable (TLab)")]
    public class Debuggable : Interactable
    {
        [SerializeField] private TextMeshProUGUI m_hoverStateDebug;

        [SerializeField] private TextMeshProUGUI m_selectStateDebug;

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            m_hoverStateDebug.text = "Hovered";
        }

        public override void Selected(Interactor interactor)
        {
            base.Selected(interactor);

            m_selectStateDebug.text = "Selected";
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            m_hoverStateDebug.text = "Un Hovered";
        }

        public override void UnSelected(Interactor interactor)
        {
            base.UnSelected(interactor);

            m_selectStateDebug.text = "Un Selected";
        }

        public override void WhileHovered(Interactor interactor)
        {
            base.WhileHovered(interactor);

            m_hoverStateDebug.text = "While Hovered";
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);

            m_selectStateDebug.text = "While Selected";
        }
    }
}
