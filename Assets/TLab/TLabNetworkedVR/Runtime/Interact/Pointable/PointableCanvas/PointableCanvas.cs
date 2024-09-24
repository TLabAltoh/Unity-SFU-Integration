using System.Collections.Generic;
using UnityEngine;

namespace TLab.NetworkedVR.Interact
{
    [AddComponentMenu("TLab/NetworkedVR/Pointable Canvas (TLab)")]
    public class PointableCanvas : Pointable
    {
        #region REGISTRY

        private static List<PointableCanvas> m_registry = new List<PointableCanvas>();

        public static new List<PointableCanvas> registry => m_registry;

        public static void Register(PointableCanvas pointableCanvas)
        {
            if (!m_registry.Contains(pointableCanvas))
            {
                m_registry.Add(pointableCanvas);
            }
        }

        public static void UnRegister(PointableCanvas pointableCanvas)
        {
            if (m_registry.Contains(pointableCanvas))
            {
                m_registry.Remove(pointableCanvas);
            }
        }

        #endregion

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

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);
        }

        public override void WhileHovered(Interactor interactor)
        {
            base.WhileHovered(interactor);
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);
        }

        private void Register()
        {
            CanvasModule.RegisterPointableCanvas(this);

            PointableCanvas.Register(this);

            m_registered = true;
        }

        private void Unregister()
        {
            if (!m_registered)
            {
                return;
            }

            CanvasModule.UnregisterPointableCanvas(this);

            PointableCanvas.UnRegister(this);

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
