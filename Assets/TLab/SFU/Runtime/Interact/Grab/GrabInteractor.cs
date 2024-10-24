using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<GrabInteractable>;

    [AddComponentMenu("TLab/SFU/Grab Interactor (TLab)")]
    public class GrabInteractor : Interactor
    {
        [Header("Grab Settings")]
        [SerializeField] private float m_hoverThreshold = 0.05f;

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

            m_pressed = m_interactDataSource.grabbed;

            m_onPress = m_interactDataSource.onGrab;

            m_onRelease = m_interactDataSource.onFree;

            m_angulerVelocity = m_interactDataSource.angulerVelocity;

            m_rotateVelocity = m_angulerVelocity;
        }
    }
}
