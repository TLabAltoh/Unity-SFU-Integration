using UnityEngine;
using TLab.VRProjct.UI;

namespace TLab.VRProjct
{
    public class JoystickPlayerController : BasePlayerController
    {
        [SerializeField] private OnScreenStick m_stick;

        protected void DoRotate()
        {
            var moveVelocity = m_controller.velocity;
            moveVelocity.y = 0;

            float value = Mathf.Min(1, m_rotateSpeed * Time.deltaTime / Vector3.Angle(transform.forward, moveVelocity));
            Vector3 newForward = Vector3.Slerp(transform.forward, moveVelocity, value);

            if (newForward != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(newForward, transform.up);
        }

        protected override void Update()
        {
            base.Update();

            Vector3 forward = Vector3.zero, right = Vector3.zero;

            var eye = Camera.main.transform;

            if (m_onGround)
            {
                forward = Vector3.ProjectOnPlane(eye.forward, m_raycastHit.normal).normalized;
                right = -Vector3.Cross(forward, Vector3.up).normalized;
            }

            var targetMove = forward * m_stick.value.y + right * m_stick.value.x;
            var targetJump = new Vector3(0.0f, m_currentJumpVelocity, 0.0f);

            m_controller.Move((targetMove * m_moveSpeed + targetJump) * Time.deltaTime);

            DoRotate();
        }
    }
}
