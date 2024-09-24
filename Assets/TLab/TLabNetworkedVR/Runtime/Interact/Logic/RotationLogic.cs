using UnityEngine;

namespace TLab.NetworkedVR.Interact
{
    [System.Serializable]
    public class RotationLogic
    {
        [SerializeField] private bool m_enabled = true;

        [SerializeField] private bool m_smooth = false;

        [SerializeField]
        [Range(0.01f, 1f)]
        private float m_lerp = 0.1f;

        //[SerializeField] private bool m_trackX = true;
        //[SerializeField] private bool m_trackY = true;
        //[SerializeField] private bool m_trackZ = true;

        private Interactor m_mainHand;
        private Interactor m_subHand;

        private Transform m_targetTransform;
        private Rigidbody m_targetRigidbody;

        private Quaternion m_mainQuaternionStart;
        private Quaternion m_thisQuaternionStart;

        public bool enabled { get => m_enabled; set => m_enabled = value; }

        public bool smooth
        {
            get => m_smooth;
            set
            {
                m_smooth = value;
            }
        }

        public float lerp
        {
            get => m_lerp;
            set
            {
                m_lerp = Mathf.Clamp(0.01f, 1f, value);
            }
        }

        public void OnMainHandGrabbed(Interactor interactor)
        {
            m_mainHand = interactor;

            m_mainQuaternionStart = m_mainHand.pointer.rotation;
            m_thisQuaternionStart = m_targetTransform.rotation;
        }

        public void OnSubHandGrabbed(Interactor interactor)
        {
            m_subHand = interactor;
        }

        public void OnMainHandReleased(Interactor interactor)
        {
            if (m_mainHand == interactor)
            {
                m_mainHand = null;
            }
        }

        public void OnSubHandReleased(Interactor interactor)
        {
            if (m_subHand == interactor)
            {
                m_subHand = null;
            }
        }

        public void UpdateOneHandLogic()
        {
            if (m_enabled && m_mainHand != null)
            {
                Quaternion deltaQuaternion;

                if (m_smooth)
                {
                    deltaQuaternion = Quaternion.Lerp(
                        Quaternion.identity * m_targetTransform.rotation * Quaternion.Inverse(m_mainQuaternionStart),
                        Quaternion.identity * m_mainHand.pointer.rotation * Quaternion.Inverse(m_mainQuaternionStart),
                        m_lerp);
                }
                else
                {
                    // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                    deltaQuaternion = Quaternion.identity * m_mainHand.pointer.rotation * Quaternion.Inverse(m_mainQuaternionStart);
                }

                if (m_targetRigidbody != null)
                {
                    m_targetRigidbody.MoveRotation(deltaQuaternion * m_thisQuaternionStart);
                }
                else
                {
                    m_targetTransform.rotation = deltaQuaternion * m_thisQuaternionStart;
                }
            }
        }

        public void Start(Transform targetTransform, Rigidbody targetRigidbody = null)
        {
            m_targetTransform = targetTransform;
            m_targetRigidbody = targetRigidbody;

            enabled = m_enabled;
        }
    }
}
