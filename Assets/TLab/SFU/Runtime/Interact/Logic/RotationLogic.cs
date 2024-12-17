using UnityEngine;

namespace TLab.SFU.Interact
{
    [System.Serializable]
    public class RotationLogic
    {
        [SerializeField] private bool m_enabled = true;

        [SerializeField] private bool m_smooth = false;

        [SerializeField, Range(0.01f, 1f)] private float m_lerp = 0.1f;

        private Interactor m_firstHand;
        private Interactor m_secondHand;

        private Transform m_transform;
        private Rigidbody m_rigidbody;

        private Quaternion m_firstHandQuaternionStart;
        private Quaternion m_thisQuaternionStart;

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

            m_firstHandQuaternionStart = m_firstHand.pointer.rotation;
            m_thisQuaternionStart = m_transform.rotation;
        }

        public void OnSecondHandEnter(Interactor interactor)
        {
            m_secondHand = interactor;
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

        public void UpdateOneHandLogic()
        {
            if (m_enabled && m_firstHand != null)
            {
                Quaternion deltaQuaternion;

                if (m_smooth)
                {
                    var newRot0 = Quaternion.identity * m_transform.rotation * Quaternion.Inverse(m_firstHandQuaternionStart);
                    var newRot1 = Quaternion.identity * m_firstHand.pointer.rotation * Quaternion.Inverse(m_firstHandQuaternionStart);
                    deltaQuaternion = Quaternion.Lerp(newRot0, newRot1, m_lerp);
                }
                else
                {
                    // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                    deltaQuaternion = Quaternion.identity * m_firstHand.pointer.rotation * Quaternion.Inverse(m_firstHandQuaternionStart);
                }

                //if (m_rigidbody)
                //    m_rigidbody.MoveRotation(deltaQuaternion * m_thisQuaternionStart);
                //else
                //    m_transform.rotation = deltaQuaternion * m_thisQuaternionStart;

                m_transform.rotation = deltaQuaternion * m_thisQuaternionStart;
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
