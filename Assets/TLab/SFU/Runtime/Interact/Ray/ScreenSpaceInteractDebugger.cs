using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<RayInteractable>;

    [AddComponentMenu("TLab/SFU/Screen Space Interact Debugger (TLab)")]
    public class ScreenSpaceInteractDebugger : Interactor
    {
        [Header("Raycast Settings")]
        [SerializeField] private float m_maxDistance = 5.0f;
        [SerializeField] private float m_rotateBias = 5.0f;

        private Vector3 m_prevRayDir = Vector3.zero;
        private Vector3 m_rayDir = Vector3.zero;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public float rotateBias { get => m_rotateBias; set => m_rotateBias = value; }

        public Ray ray => Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);

        [Header("Move Settings")]
        [SerializeField] public float navigationSpeed = 2.4f;
        [SerializeField] public float shiftMultiplier = 2f;
        [SerializeField] public float sensitivity = 1.0f;
        [SerializeField] public float panSensitivity = 0.5f;
        [SerializeField] public float mouseWheelZoomSpeed = 1.0f;

        [Header("Debug In Editor")]

        [SerializeField] private GameObjectController[] m_controllers;

        private Vector3 m_prevMousePosition;
        private Quaternion m_prevTransformRotation;

        private bool m_isPanning;

#if UNITY_EDITOR
        public void Grab()
        {
            foreach (var controller in m_controllers)
                controller.OnGrab(this);
        }

        public void Release() => m_controllers.Foreach((c) => c.OnRelease(this));
#endif

        protected override void UpdateRaycast()
        {
            m_candidate = null;

            var minDist = float.MaxValue;

            var ray = this.ray;

            Registry.registry.ForEach((h) =>
            {
                if (h.Raycast(ray, out m_raycastHit, m_maxDistance))
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
            m_pressed = UnityEngine.Input.GetMouseButton(0);

            m_onPress = UnityEngine.Input.GetMouseButtonDown(0);

            m_onRelease = UnityEngine.Input.GetMouseButtonUp(0);

            m_pointer.position = (m_interactable != null) ?
                this.ray.GetPoint(m_raycastHit.distance) : m_pointer.position;

            m_prevRayDir = m_rayDir;
            m_rayDir = this.ray.direction;

            var angle = Vector3.Angle(m_rayDir, m_prevRayDir);
            var cross = Vector3.Cross(m_rayDir, m_prevRayDir);

            m_angulerVelocity = -cross.normalized * (angle / Time.deltaTime);

            m_rotateVelocity = m_angulerVelocity * m_rotateBias;
        }

        protected override void Update()
        {
            base.Update();

            MousePanning();
            if (m_isPanning)
                return;

            if (UnityEngine.Input.GetMouseButton(1))
            {
                var move = Vector3.zero;
                var speed = navigationSpeed * (UnityEngine.Input.GetKey(KeyCode.LeftShift) ? shiftMultiplier : 1f) * Time.deltaTime * 9.1f;
                if (UnityEngine.Input.GetKey(KeyCode.W))
                    move += Vector3.forward * speed;
                if (UnityEngine.Input.GetKey(KeyCode.S))
                    move -= Vector3.forward * speed;
                if (UnityEngine.Input.GetKey(KeyCode.D))
                    move += Vector3.right * speed;
                if (UnityEngine.Input.GetKey(KeyCode.A))
                    move -= Vector3.right * speed;
                if (UnityEngine.Input.GetKey(KeyCode.E))
                    move += Vector3.up * speed;
                if (UnityEngine.Input.GetKey(KeyCode.Q))
                    move -= Vector3.up * speed;

                transform.Translate(move);
            }

            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                m_prevMousePosition = new Vector3(UnityEngine.Input.mousePosition.y, -UnityEngine.Input.mousePosition.x);
                m_prevTransformRotation = transform.rotation;
            }

            if (UnityEngine.Input.GetMouseButton(1))
            {
                var rot = m_prevTransformRotation;
                var dif = m_prevMousePosition - new Vector3(UnityEngine.Input.mousePosition.y, -UnityEngine.Input.mousePosition.x);
                rot.eulerAngles += dif * sensitivity;
                transform.rotation = rot;
            }

            MouseWheeling();
        }

        void MouseWheeling()
        {
            // Zoom with mouse wheel

            var speed = 10 * (mouseWheelZoomSpeed * (UnityEngine.Input.GetKey(KeyCode.LeftShift) ? shiftMultiplier : 1f) * Time.deltaTime * 9.1f);

            var pos = transform.position;
            if (UnityEngine.Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                pos = pos - (Camera.main.transform.forward * speed);
                transform.position = pos;
            }
            if (UnityEngine.Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                pos = pos + (Camera.main.transform.forward * speed);
                transform.position = pos;
            }
        }

        private float m_panX;
        private float m_panY;
        private Vector3 m_panComplete;

        void MousePanning()
        {
            m_panX = -UnityEngine.Input.GetAxis("Mouse X") * panSensitivity;
            m_panY = -UnityEngine.Input.GetAxis("Mouse Y") * panSensitivity;

            m_panComplete = new Vector3(m_panX, m_panY, 0);

            if (UnityEngine.Input.GetMouseButtonDown(2))
                m_isPanning = true;

            if (UnityEngine.Input.GetMouseButtonUp(2))
                m_isPanning = false;

            if (m_isPanning)
                transform.Translate(m_panComplete);
        }
    }
}
