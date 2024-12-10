using UnityEngine;

namespace TLab.VRProjct
{
    public class OVRPlayerController : BasePlayerController
    {
        [Header("OVR Input")]
        [SerializeField] private OVRInput.Controller m_moveController;
        [SerializeField] private OVRInput.Button m_jumpButton = OVRInput.Button.One;
        [SerializeField] private OVRInput.Button m_runButton = OVRInput.Button.Two;

        protected override void Update()
        {
            base.Update();

            if (OVRInput.GetDown(m_jumpButton))
                Jump();

            var input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, m_moveController);

            var eye = Camera.main.transform;

            var targetForward = new Vector3(eye.forward.x, 0.0f, eye.forward.z).normalized;
            var targetRight = new Vector3(eye.right.x, 0.0f, eye.right.z).normalized;
            var targetMove = targetForward * input.y + targetRight * input.x;
            var targetJump = new Vector3(0.0f, m_currentJumpVelocity, 0.0f);

            var final = OVRInput.Get(m_runButton) ? m_runSpeed : m_moveSpeed;

            m_controller.Move((targetMove * final + targetJump) * Time.deltaTime);
        }
    }
}
