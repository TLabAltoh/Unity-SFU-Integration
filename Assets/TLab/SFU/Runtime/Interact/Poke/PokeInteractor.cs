using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<PokeInteractable>;

    [AddComponentMenu("TLab/SFU/Poke Interactor (TLab)")]
    class PokeInteractor : Interactor
    {
        [Header("Poke Settings")]
        [SerializeField] private float m_hoverThreshold = 0.05f;
        [SerializeField] private float m_selectThreshold = 0.01f;

        [Header("Target Gesture")]
        [Tooltip("Gestures to indicate ready to poke interact")]
        [SerializeField] private string m_gesture;

        private Vector3 m_rayDir;

        private Vector3 m_prevRayDir;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected override void UpdateRaycast()
        {
            base.UpdateRaycast();

            m_candidate = null;

            var minDist = float.MaxValue;

            Registry.registry.ForEach((h) =>
            {
                if (h.Spherecast(m_pointer.position, out m_raycastHit, m_hoverThreshold))
                {
                    var tmp = m_raycastHit.distance;

                    if (minDist > tmp)
                    {
                        m_candidate = h;
                        minDist = tmp;
                    }
                }
            });
        }

        protected override void UpdateInput()
        {
            base.UpdateInput();

            m_pressed = (m_interactable != null) ?
                m_interactable.Spherecast(m_pointer.position, out var raycastHit, m_selectThreshold) : false;

            m_onPress = !m_onPress && m_pressed;

            m_onRelease = m_onPress && !m_pressed;

            m_prevRayDir = m_rayDir;
            m_rayDir = (m_pointer.position - m_interactDataSource.rootPose.position).normalized;

            var angle = Vector3.Angle(m_rayDir, m_prevRayDir);
            var cross = Vector3.Cross(m_rayDir, m_prevRayDir);

            m_angulerVelocity = -cross.normalized * (angle / Time.deltaTime);

            m_rotateVelocity = m_angulerVelocity;
        }

        protected override void Process()
        {
            if (m_interactDataSource.currentGesture == m_gesture)
            {
                base.Process();
            }
            else
            {
                ForceClear();
            }
        }
    }
}
