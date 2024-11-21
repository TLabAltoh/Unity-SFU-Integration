using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network
{
    using Registry = Registry<NetworkTransform>;

    [AddComponentMenu("TLab/SFU/Network Transform (TLab)")]
    [CanEditMultipleObjects]
    public class NetworkTransform : NetworkObject
    {
        #region STRUCT

        [System.Serializable]
        public struct RigidbodyState
        {
            public static bool operator ==(RigidbodyState a, RigidbodyState b) => (a.used == b.used) && (a.gravity == b.gravity);
            public static bool operator !=(RigidbodyState a, RigidbodyState b) => (a.used != b.used) || (a.gravity != b.gravity);

            [SerializeField] private bool m_used;

            [SerializeField] private bool m_gravity;

            public bool used => m_used;

            public bool gravity => m_gravity;

            public RigidbodyState(bool used, bool gravity)
            {
                m_used = used;
                m_gravity = gravity;
            }

            public void Update(bool used, bool gravity)
            {
                m_used = used;
                m_gravity = gravity;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is not RigidbodyState)
                    return false;

                var tmp = (RigidbodyState)obj;
                return this == tmp;
            }
        }

        [System.Serializable]
        public struct WebTransform
        {
            public Address64 id;
            public RigidbodyState rb;
            public Vector3 position;
            public Vector4 rotation;
            public Vector3 localScale;
        }

        #endregion STRUCT

        #region MESSAGE

        [System.Serializable]
        public class MSG_SyncTransform : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_SyncTransform() => pktId = MD5From(nameof(MSG_SyncTransform));

            public WebTransform transform;

            public const int HEADER_LEN = 8;

            public const int PAYLOAD_LEN = 2 + 10 * sizeof(float);  // rbState.used (1) + rbState.gravity (1) + transform ((3 + 4 + 3) * 4)

            public MSG_SyncTransform() : base() { }

            public MSG_SyncTransform(byte[] bytes) : base(bytes) { }

            public override byte[] Marshall()
            {
                var rtcTransform = new float[10];

                rtcTransform[0] = transform.position.x;
                rtcTransform[1] = transform.position.y;
                rtcTransform[2] = transform.position.z;

                rtcTransform[3] = transform.rotation.x;
                rtcTransform[4] = transform.rotation.y;
                rtcTransform[5] = transform.rotation.z;
                rtcTransform[6] = transform.rotation.w;

                rtcTransform[7] = transform.localScale.x;
                rtcTransform[8] = transform.localScale.y;
                rtcTransform[9] = transform.localScale.z;

                var packet = new byte[HEADER_LEN + PAYLOAD_LEN];

                unsafe
                {
                    fixed (byte* packetPtr = packet)
                    {
                        transform.id.CopyTo(packetPtr);

                        bool rbUsed = transform.rb.used, rbGravity = transform.rb.gravity;

                        packetPtr[HEADER_LEN + 0] = (byte)(&rbUsed);
                        packetPtr[HEADER_LEN + 1] = (byte)(&rbGravity);

                        fixed (float* transformPtr = &(rtcTransform[0]))
                            UnsafeUtility.LongCopy((byte*)transformPtr, packetPtr + HEADER_LEN + 2, PAYLOAD_LEN - 2);
                    }
                }

                return packet;
            }

            public override void UnMarshall(byte[] bytes)
            {
                float[] rtcTransform = new float[10];

                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    fixed (float* rtcTransformPtr = &(rtcTransform[0]))
                    {
                        UnsafeUtility.LongCopy(bytesPtr + HEADER_LEN + 2, (byte*)rtcTransformPtr, PAYLOAD_LEN - 2);

                        transform.id.Copy(bytesPtr);
                        transform.rb.Update(*((bool*)&(bytesPtr[HEADER_LEN + 0])), *((bool*)&(bytesPtr[HEADER_LEN + 1])));

                        transform.position.x = rtcTransform[0];
                        transform.position.y = rtcTransform[1];
                        transform.position.z = rtcTransform[2];

                        transform.rotation.x = rtcTransform[3];
                        transform.rotation.y = rtcTransform[4];
                        transform.rotation.z = rtcTransform[5];
                        transform.rotation.w = rtcTransform[6];

                        transform.localScale.x = rtcTransform[7];
                        transform.localScale.y = rtcTransform[8];
                        transform.localScale.z = rtcTransform[9];
                    }
                }
            }
        }

        #endregion MESSAGE

        public float positionThreshold = 0.001f;

        [Range(0.00001f, 360.0f)]
        public float rotAngleThreshold = 0.01f;

        public float scaleThreshold = 0.01f;

        protected Rigidbody m_rb;

        protected RigidbodyState m_rbState;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        // Windows 12's Core i 9: 400 -----> Size: 20
        protected const int CASH_COUNT = 20;
#else
        // Oculsu Quest 2: 72 -----> Size: 20 * 72 / 400 = 3.6 ~= 4
        protected const int CASH_COUNT = 5;
#endif

        protected FixedQueue<(Vector3, Quaternion)> m_rbHistory = new FixedQueue<(Vector3, Quaternion)>(CASH_COUNT);

        protected WebTransform m_networkState;

        public Rigidbody rb => m_rb;

        public RigidbodyState rbState => m_rbState;

        public bool enableGravity => (m_rb == null) ? false : m_rb.useGravity;

        public static bool msgCallbackRegisted = false;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

#if UNITY_EDITOR
        public virtual void UseRigidbody(bool rigidbody, bool gravity)
        {
            if (EditorApplication.isPlaying)
                return;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = gravity;
        }
#endif

        protected void EstimateRbVelocity(out Vector3 velocity, out Vector3 angularVelocity)
        {
            if (m_rbHistory.Count < 2)
            {
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
                return;
            }

            var history = m_rbHistory.ToArray();

            m_rbHistory.Clear();

            var positionDiff = Vector3.zero;
            var rotationDiff = Vector3.zero;

            for (int i = 1; i < history.Length; i++)
            {
                positionDiff += history[i].Item1 - history[i - 1].Item1;

                var tmp = Quaternion.Inverse(history[i - 1].Item2) * history[i].Item2;
                tmp.ToAngleAxis(out var angle, out var axis);

                rotationDiff += (m_rb.rotation * axis) * angle;
            }

            velocity = positionDiff / (history.Length - 1) / Time.deltaTime;
            angularVelocity = rotationDiff / (history.Length - 1) / Time.deltaTime;
        }

        public virtual void OnPhysicsRoleChange()
        {
            switch (NetworkClient.physicsRole)
            {
                case NetworkClient.PhysicsRole.SEND:
                    EnableRigidbody(true);
                    break;
                case NetworkClient.PhysicsRole.RECV:
                    EnableRigidbody(false, true);
                    break;
            }
        }

        public virtual void EnableRigidbody(bool active, bool force = false)
        {
            if (m_rb == null)
                return;

            if (active && m_rbState.gravity)
            {
                m_rb.isKinematic = false;
                m_rb.useGravity = true;

                EstimateRbVelocity(out var velocity, out var angularVelocity);
                m_rb.velocity = velocity;
                m_rb.angularVelocity = angularVelocity;
            }
            else
            {
                m_rb.isKinematic = true;
                m_rb.useGravity = false;
                m_rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        public void SyncFromOutside(WebTransform transform)
        {
            var position = transform.position;
            var rotation = transform.rotation;
            var localScale = transform.localScale;

            this.transform.localScale = new Vector3(localScale.x, localScale.y, localScale.z);

            if (m_rb != null)
            {
                m_rb.MovePosition(new Vector3(position.x, position.y, position.z));
                m_rb.MoveRotation(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
            }
            else
            {
                this.transform.position = new Vector3(position.x, position.y, position.z);
                this.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            }

            UpdateRbHistory();

            ApplyCurrentTransform();

            m_synchronised = true;
        }

        public virtual bool ApplyCurrentTransform()
        {
            bool isDirty = false;

            m_networkState.id = m_networkId.id;

            if (m_networkState.rb != m_rbState)
            {
                m_networkState.rb = m_rbState;
                isDirty = true;
            }

            if (Vector3.Distance(m_networkState.position, this.transform.position) > positionThreshold)
            {
                m_networkState.position.x = this.transform.position.x;
                m_networkState.position.y = this.transform.position.y;
                m_networkState.position.z = this.transform.position.z;
                isDirty = true;
            }

            if (Quaternion.Angle(m_networkState.rotation.ToQuaternion(), this.transform.rotation) > rotAngleThreshold)
            {
                m_networkState.rotation.x = this.transform.rotation.x;
                m_networkState.rotation.y = this.transform.rotation.y;
                m_networkState.rotation.z = this.transform.rotation.z;
                m_networkState.rotation.w = this.transform.rotation.w;
                isDirty = true;
            }

            if (Vector3.Distance(m_networkState.localScale, this.transform.localScale) > scaleThreshold)
            {
                m_networkState.localScale.x = this.transform.localScale.x;
                m_networkState.localScale.y = this.transform.localScale.y;
                m_networkState.localScale.z = this.transform.localScale.z;
                isDirty = true;
            }

            return isDirty;
        }

        public override void SyncViaWebRTC()
        {
            if (!Const.SEND.HasFlag(m_direction) || (m_state != State.INITIALIZED))
                return;

            if (ApplyCurrentTransform())
            {
                var @object = new MSG_SyncTransform
                {
                    transform = m_networkState,
                };

                NetworkClient.instance.SendRTC(@object.Marshall());

                m_synchronised = false;
            }
        }

        public override void SyncViaWebSocket()
        {
            if (!Const.SEND.HasFlag(m_direction) || (m_state != State.INITIALIZED))
                return;

            if (ApplyCurrentTransform())
            {
                var @object = new MSG_SyncTransform
                {
                    transform = m_networkState,
                };

                NetworkClient.instance.SendWS(@object.Marshall());

                m_synchronised = false;
            }
        }

        protected virtual void UpdateRbHistory()
        {
            if (m_rb != null)
                m_rbHistory.Enqueue((m_rb.position, m_rb.rotation));
        }

        protected virtual void InitRigidbody()
        {
            m_rb = GetComponent<Rigidbody>();

            if (m_rb != null)
            {
                m_rbHistory.Enqueue((m_rb.position, m_rb.rotation));

                m_rbState = new RigidbodyState(true, m_rb.useGravity);

                EnableRigidbody(false, true);
            }
            else
                m_rbState = new RigidbodyState(false, false);
        }

        public override void Shutdown()
        {
            if (m_state == State.SHUTDOWNED)
                return;

            if (m_networkId)
                Registry.UnRegister(m_networkId.id);

            base.Shutdown();
        }

        public override void Init(Address32 publicId)
        {
            base.Init(publicId);

            Registry.Register(m_networkId.id, this);
        }

        public override void Init()
        {
            base.Init();

            Registry.Register(m_networkId.id, this);
        }

        protected override void Awake()
        {
            base.Awake();

            if (!msgCallbackRegisted)
            {
                NetworkClient.RegisterOnMessage(MSG_SyncTransform.pktId, (from, to, bytes) =>
                {
                    var @object = new MSG_SyncTransform(bytes);
                    Registry.GetById(@object.transform.id)?.SyncFromOutside(@object.transform);
                });
                msgCallbackRegisted = true;
            }
        }

        protected override void Start()
        {
            base.Start();

            InitRigidbody();
        }

        protected override void Update()
        {
            UpdateRbHistory();

            SyncViaWebRTC();
        }

        protected override void Register()
        {
            base.Register();

            Registry.Register(m_networkId.id, this);
        }

        protected override void UnRegister()
        {
            Registry.UnRegister(m_networkId.id);

            base.UnRegister();
        }
    }
}
