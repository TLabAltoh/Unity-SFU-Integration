using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<GameObjectRotatable>;

    [AddComponentMenu("TLab/SFU/Game Object Rotatable (TLab)")]
    [RequireComponent(typeof(GameObjectController))]
    public class GameObjectRotatable : Interactable
    {
        private GameObjectController m_controller;

        private Vector3 m_axis;

        private float m_angle;

        private bool m_onShot = false;

        private const float DURATION = 0.1f;

        public static float ZERO_ANGLE = 0.0f;

        private bool grabbed => m_controller.grabState.grabbed;

        private bool synchronised => m_controller.synchronised;

        public void Stop()
        {
            if (!grabbed)
            {
                m_axis = -Vector3.one;
                m_angle = ZERO_ANGLE;

                m_onShot = false;
            }
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);

            if (interactor.pressed || !grabbed)
            {
                var angulerVel = interactor.rotateVelocity;

                m_axis = -angulerVel.normalized;
                m_angle = angulerVel.magnitude * Time.deltaTime;

                m_onShot = true;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Registry.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Registry.UnRegister(this);
        }

        protected override void Start()
        {
            base.Start();

            m_controller = GetComponent<GameObjectController>();
        }

        protected override void Update()
        {
            base.Update();

            if ((m_controller == null || !grabbed) && (!synchronised || m_onShot) && m_angle > ZERO_ANGLE)
            {
                transform.rotation = Quaternion.AngleAxis(m_angle, m_axis) * transform.rotation;
                m_angle = Mathf.Clamp(m_angle - DURATION * Time.deltaTime, ZERO_ANGLE, float.MaxValue);

                m_controller?.SyncViaWebRTC();
            }
            else
                m_angle = ZERO_ANGLE;

            m_onShot = false;
        }
    }
}
