using UnityEngine;

namespace TLab.NetworkedVR.Interact
{
    [AddComponentMenu("TLab/NetworkedVR/Screen Space Ray Interactor (TLab)")]
    public class ScreenSpaceRayInteractor : Interactor
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
        [SerializeField] float navigationSpeed = 2.4f;
        [SerializeField] float shiftMultiplier = 2f;
        [SerializeField] float sensitivity = 1.0f;
        [SerializeField] float panSensitivity = 0.5f;
        [SerializeField] float mouseWheelZoomSpeed = 1.0f;

        [Header("Debug In Editor")]

        [SerializeField] private GameObjectController[] m_controllers;

        private Camera cam;
        private Vector3 anchorPoint;
        private Quaternion anchorRot;

        private bool isPanning;

        public void Grab()
        {
            foreach (var controller in m_controllers)
            {
                controller.OnGrabbed(this);
            }
        }

        public void Release()
        {
            foreach (var controller in m_controllers)
            {
                controller.OnRelease(this);
            }
        }

        protected override void UpdateRaycast()
        {
            base.UpdateRaycast();

            m_candidate = null;

            var minDist = float.MaxValue;

            var ray = this.ray;

            RayInteractable.registry.ForEach((h) =>
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
            base.UpdateInput();

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

        protected override void Awake()
        {
            base.Awake();

            cam = GetComponent<Camera>();
        }

        protected override void Update()
        {
            base.Update();

            MousePanning();
            if (isPanning)
            { return; }

            if (UnityEngine.Input.GetMouseButton(1))
            {
                Vector3 move = Vector3.zero;
                float speed = navigationSpeed * (UnityEngine.Input.GetKey(KeyCode.LeftShift) ? shiftMultiplier : 1f) * Time.deltaTime * 9.1f;
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

                Camera.main.transform.Translate(move);
            }

            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                anchorPoint = new Vector3(UnityEngine.Input.mousePosition.y, -UnityEngine.Input.mousePosition.x);
                anchorRot = Camera.main.transform.rotation;
            }

            if (UnityEngine.Input.GetMouseButton(1))
            {
                Quaternion rot = anchorRot;
                Vector3 dif = anchorPoint - new Vector3(UnityEngine.Input.mousePosition.y, -UnityEngine.Input.mousePosition.x);
                rot.eulerAngles += dif * sensitivity;
                Camera.main.transform.rotation = rot;
            }

            MouseWheeling();
        }

        //Zoom with mouse wheel
        void MouseWheeling()
        {
            float speed = 10 * (mouseWheelZoomSpeed * (UnityEngine.Input.GetKey(KeyCode.LeftShift) ? shiftMultiplier : 1f) * Time.deltaTime * 9.1f);

            Vector3 pos = Camera.main.transform.position;
            if (UnityEngine.Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                pos = pos - (Camera.main.transform.forward * speed);
                Camera.main.transform.position = pos;
            }
            if (UnityEngine.Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                pos = pos + (Camera.main.transform.forward * speed);
                Camera.main.transform.position = pos;
            }
        }

        private float pan_x;
        private float pan_y;
        private Vector3 panComplete;

        void MousePanning()
        {
            pan_x = -UnityEngine.Input.GetAxis("Mouse X") * panSensitivity;
            pan_y = -UnityEngine.Input.GetAxis("Mouse Y") * panSensitivity;

            panComplete = new Vector3(pan_x, pan_y, 0);

            if (UnityEngine.Input.GetMouseButtonDown(2))
            {
                isPanning = true;
            }

            if (UnityEngine.Input.GetMouseButtonUp(2))
            {
                isPanning = false;
            }

            if (isPanning)
            {
                Camera.main.transform.Translate(panComplete);
            }
        }
    }
}
