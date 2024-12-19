using UnityEngine;
using TMPro;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Debugable (TLab)")]
    public class Debuggable : Interactable
    {
        [SerializeField] private TextMeshProUGUI m_hoverStateDebug;

        [SerializeField] private TextMeshProUGUI m_selectStateDebug;

        public override void OnHover(Interactor interactor)
        {
            base.OnHover(interactor);

            m_hoverStateDebug.text = "Hover";
        }

        public override void OnSelect(Interactor interactor)
        {
            base.OnSelect(interactor);

            m_selectStateDebug.text = "Select";
        }

        public override void OnUnhover(Interactor interactor)
        {
            base.OnUnhover(interactor);

            m_hoverStateDebug.text = "Unhover";
        }

        public override void OnUnselect(Interactor interactor)
        {
            base.OnUnselect(interactor);

            m_selectStateDebug.text = "Unselect";
        }

        public override void WhileHover(Interactor interactor)
        {
            base.WhileHover(interactor);

            m_hoverStateDebug.text = "While Hover";
        }

        public override void WhileSelect(Interactor interactor)
        {
            base.WhileSelect(interactor);

            m_selectStateDebug.text = "While Select";
        }
    }
}
