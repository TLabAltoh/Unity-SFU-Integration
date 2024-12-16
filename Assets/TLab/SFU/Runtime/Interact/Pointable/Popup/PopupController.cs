using UnityEngine;

namespace TLab.SFU.Interact
{
    public class PopupController : MonoBehaviour
    {
        [System.Serializable]
        public class PointerPopupPair
        {
            public GameObject target;
            public FloatingAnchor anchor;
        }

        public PointerPopupPair[] pointerPairs => m_pointerPairs;

        [SerializeField] protected FloatingAnchor[] m_anchors;

        [SerializeField] protected PointerPopupPair[] m_pointerPairs;

        public FloatingAnchor GetFloatingAnchor(int index)
        {
            if (index < m_pointerPairs.Length)
                return m_pointerPairs[index].anchor;
            else
                return null;
        }

        protected void OnDestroy()
        {
            if (m_anchors.Length > 0)
            {
                foreach (var anchor in m_anchors)
                {
                    if (anchor != null)
                        Destroy(anchor.gameObject);
                }
            }

            if (m_pointerPairs.Length > 0)
            {
                foreach (var pointerPair in m_pointerPairs)
                {
                    if (pointerPair.anchor != null)
                        Destroy(pointerPair.anchor.gameObject);
                }
            }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (m_pointerPairs == null)
                return;

            foreach (var popupPair in m_pointerPairs)
            {
                if (popupPair.anchor != null && popupPair.target != null)
                {
                    popupPair.anchor.SetTarget(popupPair.target.transform);
                    popupPair.anchor.SetHideOnStart();
                }
            }
        }
#endif
    }
}