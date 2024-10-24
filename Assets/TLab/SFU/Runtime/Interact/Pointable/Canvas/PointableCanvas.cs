using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Pointable Canvas (TLab)")]
    public class PointableCanvas : Pointable
    {
        public enum Surface
        {
            BACKWARD,
            FORWARD,
            BOTH
        };

        [Header("Target Canvas")]
        [SerializeField] private Canvas m_canvas;

        private bool m_started = false;
        private bool m_registered = false;

        public Canvas canvas => m_canvas;

        private void Register()
        {
            CanvasModule.RegisterPointableCanvas(this);

            Registry<PointableCanvas>.Register(this);

            m_registered = true;
        }

        private void Unregister()
        {
            if (!m_registered)
            {
                return;
            }

            CanvasModule.UnregisterPointableCanvas(this);

            Registry<PointableCanvas>.UnRegister(this);

            m_registered = false;
        }

        public override bool Spherecast(Vector3 point, out RaycastHit hit, float maxDistance)
        {
            return base.Spherecast(point, out hit, maxDistance);
        }

        protected override void Start()
        {
            this.BeginStart(ref m_started, () => base.Start());
            this.EndStart(ref m_started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_started)
            {
                Register();
            }
        }

        protected override void OnDisable()
        {
            if (m_started)
            {
                Unregister();
            }

            base.OnDisable();
        }
    }
}