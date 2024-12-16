using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<PointableCanvas>;

    [AddComponentMenu("TLab/SFU/Pointable Canvas (TLab)")]
    public class PointableCanvas : Pointable
    {
        public enum Surface
        {
            Backward,
            Forward,
            Both,
        };

        [Header("UI")]
        [SerializeField] private Canvas m_canvas;

        private bool m_started = false;
        private bool m_registed = false;

        public Canvas canvas => m_canvas;

        private void Register()
        {
            CanvasModule.RegisterPointableCanvas(this);

            Registry.Register(this);

            m_registed = true;
        }

        private void Unregister()
        {
            if (!m_registed)
                return;

            CanvasModule.UnregisterPointableCanvas(this);

            Registry.UnRegister(this);

            m_registed = false;
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
                Register();
        }

        protected override void OnDisable()
        {
            if (m_started)
                Unregister();

            base.OnDisable();
        }
    }
}
