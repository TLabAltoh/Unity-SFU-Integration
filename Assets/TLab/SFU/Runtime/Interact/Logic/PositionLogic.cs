using UnityEngine;

namespace TLab.SFU.Interact
{
    [System.Serializable]
    public class PositionLogic
    {
        [SerializeField] private bool m_enabled = true;
        [SerializeField] private bool m_smooth = false;

        [SerializeField]
        [Range(0.01f, 1f)]
        private float m_lerp = 0.1f;

        private Interactor m_firstHand;
        private Interactor m_secondHand;

        private Transform m_transform;
        private Rigidbody m_rigidbody;

        private Vector3 m_firstHandPositionOffset;
        private Vector3 m_secondHandPositionOffset;

        public bool enabled
        {
            get => m_enabled;
            set
            {
                if (m_enabled != value)
                {
                    m_enabled = value;
                }
            }
        }

        public bool smooth
        {
            get => m_smooth;
            set
            {
                if (m_smooth != value)
                {
                    m_smooth = value;
                }
            }
        }

        public float lerp
        {
            get => m_lerp;
            set
            {
                if (m_lerp != value)
                {
                    m_lerp = Mathf.Clamp(0.01f, 1f, value);
                }
            }
        }

        public void OnFirstHandEnter(Interactor interactor)
        {
            m_firstHand = interactor;

            m_firstHandPositionOffset = m_firstHand.pointer.InverseTransformPoint(m_transform.position);
        }

        public void OnSecondHandEnter(Interactor interactor)
        {
            m_secondHand = interactor;

            m_secondHandPositionOffset = m_secondHand.pointer.InverseTransformPoint(m_transform.position);
        }

        public void OnFirstHandExit(Interactor interactor)
        {
            if (m_firstHand == interactor)
                m_firstHand = null;
        }

        public void OnSecondHandExit(Interactor interactor)
        {
            if (m_secondHand == interactor)
                m_secondHand = null;
        }

        public void UpdateTwoHandLogic()
        {
            if (m_enabled && m_firstHand != null && m_secondHand != null)
            {
                Vector3 newPos0, newPos1;

                if (m_smooth)
                {
                    newPos0 = Vector3.Lerp(m_transform.position, m_firstHand.pointer.TransformPoint(m_firstHandPositionOffset), m_lerp);
                    newPos1 = Vector3.Lerp(m_transform.position, m_secondHand.pointer.TransformPoint(m_secondHandPositionOffset), m_lerp);
                }
                else
                {
                    newPos0 = m_firstHand.pointer.TransformPoint(m_firstHandPositionOffset);
                    newPos1 = m_secondHand.pointer.TransformPoint(m_secondHandPositionOffset);
                }

                var newPos = Vector3.Lerp(newPos0, newPos1, 0.5f);

                if (m_rigidbody)
                    m_rigidbody.position = newPos;
                else
                    m_transform.position = newPos;
            }
        }

        public void UpdateOneHandLogic()
        {
            if (m_enabled && m_firstHand != null)
            {
                Vector3 newPos;

                if (m_smooth)
                    newPos = Vector3.Lerp(m_transform.position, m_firstHand.pointer.TransformPoint(m_firstHandPositionOffset), m_lerp);
                else
                    newPos = m_firstHand.pointer.TransformPoint(m_firstHandPositionOffset);

                if (m_rigidbody)
                    m_rigidbody.position = newPos;
                else
                    m_transform.position = newPos;
            }
        }

        public void Start(Transform transform, Rigidbody rigidbody = null)
        {
            m_transform = transform;
            m_rigidbody = rigidbody;

            enabled = m_enabled;
        }
    }
}
