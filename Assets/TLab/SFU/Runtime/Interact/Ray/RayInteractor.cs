using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<RayInteractable>;

    [AddComponentMenu("TLab/SFU/Ray Interactor (TLab)")]
    public class RayInteractor : Interactor
    {
        [Header("Raycast Settings")]
        [SerializeField] private float m_maxDistance = 5.0f;
        [SerializeField] private float m_rotateBias = 5.0f;

        private Vector3 m_prevRayDir = Vector3.zero;
        private Vector3 m_rayDir = Vector3.zero;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public float rotateBias { get => m_rotateBias; set => m_rotateBias = value; }

        public Ray ray => new Ray(m_interactDataSource.pointerPose.position, m_interactDataSource.pointerPose.forward);

        protected override void UpdateRaycast()
        {
            m_candidate = null;

            var minDist = float.MaxValue;

            var ray = this.ray;

            Registry.registry.ForEach((h) =>
            {
                if (h.Raycast(ray, out m_raycastHit, m_maxDistance))
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
            m_pressed = m_interactDataSource.pressed;

            m_onPress = m_interactDataSource.onPress;

            m_onRelease = m_interactDataSource.onRelease;

            m_pointer.position = (m_interactable != null) ?
                this.ray.GetPoint(m_raycastHit.distance) : m_pointer.position;

            m_prevRayDir = m_rayDir;
            m_rayDir = m_interactDataSource.pointerPose.forward;

            var angle = Vector3.Angle(m_rayDir, m_prevRayDir);
            var cross = Vector3.Cross(m_rayDir, m_prevRayDir);

            m_angulerVelocity = -cross.normalized * (angle / Time.deltaTime);

            m_rotateVelocity = m_angulerVelocity * m_rotateBias;
        }
    }
}