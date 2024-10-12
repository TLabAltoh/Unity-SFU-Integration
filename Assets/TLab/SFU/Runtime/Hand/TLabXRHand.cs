using UnityEngine;
using TLab.SFU.Input;

namespace TLab.SFU
{
    [AddComponentMenu("TLab/SFU/TLab XR Hand (TLab)")]
    public class TLabXRHand : MonoBehaviour
    {
        [SerializeField] private InputDataSource m_inputDataSource;

        private Quaternion m_prevHandRotation;
        private Quaternion m_handRotation;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public InputDataSource inputDataSource => m_inputDataSource;

        public string currentGesture => m_inputDataSource.currentGesture;

        public Pose pointerPose => m_inputDataSource.pointerPose;

        public Pose rootPose => m_inputDataSource.rootPose;

        public Vector3 angulerVelocity
        {
            get
            {
                // https://nekojara.city/unity-object-angular-velocity
                var diffRotation = Quaternion.Inverse(m_prevHandRotation) * m_handRotation;

                diffRotation.ToAngleAxis(out var angle, out var axis);

                return m_handRotation.normalized * axis * (angle / Time.deltaTime);
            }
        }

        public float pressStrength => m_inputDataSource.pressStrength;

        public bool pressed => m_inputDataSource.pressed;

        public bool onPress => m_inputDataSource.onPress;

        public bool onRelease => m_inputDataSource.onRelease;

        public float grabStrength => m_inputDataSource.grabStrength;

        public bool grabbed => m_inputDataSource.grabbed;

        public bool onGrab => m_inputDataSource.onGrab;

        public bool onFree => m_inputDataSource.onFree;

        void Update()
        {
            m_prevHandRotation = m_handRotation;
            m_handRotation = m_inputDataSource.rootPose.rotation;
        }
    }
}
