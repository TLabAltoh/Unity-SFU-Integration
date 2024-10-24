using UnityEngine;

namespace TLab.VRClassroom
{
    public class PlayerController : BasePlayerController
    {
        [Header("Input Config")]
        [SerializeField] private OVRInput.Controller m_moveController;
        [SerializeField] private OVRInput.Button m_jumpButton = OVRInput.Button.One;
        [SerializeField] private OVRInput.Button m_runButton = OVRInput.Button.Two;

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            if (OVRInput.GetDown(m_jumpButton))
            {
                Jump();
            }

            var input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, m_moveController);

            var targetForward = new Vector3(m_directionAnchor.forward.x, ZERO, m_directionAnchor.forward.z).normalized;
            var targetRight = new Vector3(m_directionAnchor.right.x, ZERO, m_directionAnchor.right.z).normalized;
            var targetMove = targetForward * input.y + targetRight * input.x;
            var targetJump = new Vector3(ZERO, m_currentJumpVelocity, ZERO);

            var final = OVRInput.Get(m_runButton) ? m_runSpeed : m_moveSpeed;

            m_controller.Move((targetMove * final + targetJump) * Time.deltaTime);
        }
    }
}
