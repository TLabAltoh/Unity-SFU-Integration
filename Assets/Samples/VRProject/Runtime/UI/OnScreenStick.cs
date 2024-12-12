/***
* This code is adapted and modified from
* https://github.com/yamara-mh/GenericCodes/blob/main/Codes/OnScreenStickCustom.cs
**/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TLab.VRProjct.UI
{
    public class OnScreenStick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private float m_range = 10f;
        [SerializeField, Range(0f, 1f)] private float m_deadZone = 0.1f;

        private Image m_bgImage, m_stickImage;

        private Transform m_bgImageTransform, m_stickImageTransform;

        private Vector2 m_value, m_center;

        public Vector2 value => m_value;

        private string THIS_NAME => "[" + GetType() + "] ";

        private void Awake()
        {
            m_bgImage = GetComponent<Image>();

            if (!m_bgImage)
            {
                Debug.LogWarning(THIS_NAME + "Image doesn't found");
                return;
            }

            if (transform.childCount == 0)
            {
                Debug.LogWarning(THIS_NAME + "child transform doesn't found");
                return;
            }

            m_stickImage = transform.GetChild(0).GetComponent<Image>();

            if (!m_stickImage)
            {
                Debug.LogWarning(THIS_NAME + "Image doesn't found");
                return;
            }

            m_bgImageTransform = m_bgImage.transform;
            m_stickImageTransform = m_stickImage.transform;

            m_center = m_stickImageTransform.position;
        }

        private void OnValueChanged(Vector2 value)
        {
            m_value = value;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_stickImageTransform.position = eventData.position;
        }
        public void OnDrag(PointerEventData eventData)
        {
            var currentRange = m_range * m_bgImageTransform.lossyScale.x;
            var vector = (Vector3)eventData.position - m_bgImageTransform.position;
            var magnitude = vector.magnitude;

            if (magnitude < currentRange * m_deadZone) vector = Vector3.zero;
            else if (magnitude > currentRange) vector *= currentRange / magnitude;

            m_stickImageTransform.position = m_bgImage.transform.position + vector;

            vector /= currentRange;

            OnValueChanged(vector);
        }
        public void OnPointerUp(PointerEventData data)
        {
            OnValueChanged(Vector2.zero);

            m_stickImageTransform.position = m_center;
        }
    }
}
