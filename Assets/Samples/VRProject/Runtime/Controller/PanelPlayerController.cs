using UnityEngine;

namespace TLab.VRProjct
{
    public class PanelPlayerController : BasePlayerController
    {
        private float m_xAxis = ZERO;

        private float m_yAxis = ZERO;

        public void OnXPress(float value) => m_xAxis = value;

        public void OnYPress(float value) => m_yAxis = value;

        public void OnXRelease(float value) => m_xAxis = value;

        public void OnYRelease(float value) => m_yAxis = value;

        protected override void Update()
        {
            base.Update();

            var forward = Vector3.zero;
            var right = Vector3.zero;

            if (m_onGround)
            {
                forward = Vector3.ProjectOnPlane(m_cameraAnchor.forward, m_raycastHit.normal).normalized;
                right = -Vector3.Cross(forward, Vector3.up).normalized;
            }

            var targetMove = forward * m_yAxis + right * m_xAxis;
            var targetJump = new Vector3(ZERO, m_currentJumpVelocity, ZERO);

            m_controller.Move((targetMove * m_moveSpeed + targetJump) * Time.deltaTime);
        }
    }
}
