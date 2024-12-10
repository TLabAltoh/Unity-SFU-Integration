using UnityEngine;

namespace TLab.VRProjct
{
    public abstract class BasePlayerController : MonoBehaviour
    {
        [SerializeField] protected CharacterController m_controller;

        [Header("Move")]
        [SerializeField] protected float m_runSpeed = 1.5f;
        [SerializeField] protected float m_moveSpeed = 1.0f;
        [SerializeField] protected float m_rotateSpeed = 35.0f;

        [Header("Jump")]
        [SerializeField] protected float m_groundThreshold = 0.2f;
        [SerializeField] protected float m_jumpHeight = 1.5f;
        [SerializeField] protected float m_jumpInertia = 0.5f;
        [SerializeField] protected float m_gravity = 1.0f;

        protected const float RAY_OFFSET = 0.1f;

        protected bool m_onGround = false;
        protected float m_currentJumpInertia = 0.0f;
        protected float m_currentJumpVelocity = 0.0f;

        protected Ray m_ray;
        protected RaycastHit m_raycastHit;

        protected const float FALLING_STABILITY = -10.0f;

        protected virtual void Jump()
        {
            if (m_onGround)
            {
                m_currentJumpVelocity = m_jumpHeight;
                m_currentJumpInertia = m_jumpInertia;
            }
        }

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            m_controller = GetComponent<CharacterController>();

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected virtual void Start() { }

        protected virtual void Update()
        {
            m_currentJumpInertia -= Time.deltaTime;
            m_currentJumpVelocity -= Time.deltaTime * m_gravity;

            m_currentJumpVelocity = m_currentJumpVelocity < FALLING_STABILITY ? FALLING_STABILITY : m_currentJumpVelocity;

            m_ray = new Ray(m_controller.transform.position + Vector3.up * RAY_OFFSET, Vector3.down);

            m_onGround = Physics.Raycast(m_ray, out m_raycastHit, m_groundThreshold);

            if (m_onGround)
                m_currentJumpVelocity = m_currentJumpInertia < 0.0f ? 0.0f : m_currentJumpVelocity;
        }
    }
}
