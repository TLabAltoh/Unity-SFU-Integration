using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using static System.BitConverter;

namespace TLab.SFU.Network
{
    using Registry = Registry<Address64, NetworkTransform>;

    [AddComponentMenu("TLab/SFU/Network Transform (TLab)")]
    public class NetworkTransform : NetworkObject
    {
        #region STRUCT

        [Serializable]
        public struct RigidbodyState
        {
            public static bool operator ==(RigidbodyState a, RigidbodyState b) => (a.used == b.used) && (a.gravity == b.gravity);
            public static bool operator !=(RigidbodyState a, RigidbodyState b) => (a.used != b.used) || (a.gravity != b.gravity);

            [SerializeField, HideInInspector] private bool m_used;

            [SerializeField, HideInInspector] private bool m_gravity;

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

        [Serializable]
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

        [Serializable, Message(typeof(MSG_SyncTransform))]
        public class MSG_SyncTransform : MSG_Sync
        {
            public WebTransform transform;

            public const int BOOL_FIELD_LEN = 2;  // requested (1) + rbState.used (1) + rbState.gravity (1)

            public const int NETWORK_ID_LEN = 8;

            public const int RTC_TRANSFORM_LEN = 10;    // transform ((3 + 4 + 3) * 4)

            public const int PAYLOAD_LEN = NETWORK_ID_LEN + BOOL_FIELD_LEN + RTC_TRANSFORM_LEN * sizeof(float);

            public MSG_SyncTransform(WebTransform transform) : base()
            {
                this.transform = transform;
            }

            public MSG_SyncTransform(byte[] bytes) : base(bytes) { }

            public override byte[] Marshall()
            {
                const int HEADER_LEN = 8;    // to (4) + msgTyp (4)

                var transformBuf = new float[RTC_TRANSFORM_LEN];

                transformBuf[0] = transform.position.x;
                transformBuf[1] = transform.position.y;
                transformBuf[2] = transform.position.z;

                transformBuf[3] = transform.rotation.x;
                transformBuf[4] = transform.rotation.y;
                transformBuf[5] = transform.rotation.z;
                transformBuf[6] = transform.rotation.w;

                transformBuf[7] = transform.localScale.x;
                transformBuf[8] = transform.localScale.y;
                transformBuf[9] = transform.localScale.z;

                var packetBuf = new byte[HEADER_LEN + PAYLOAD_LEN];

                unsafe
                {
                    fixed (byte* packetBufPtr = packetBuf)
                    {
                        bool rbUsed = transform.rb.used, rbGravity = transform.rb.gravity, requested = this.requested;

                        packetBufPtr[HEADER_LEN + NETWORK_ID_LEN + 0] = (byte)(&requested);
                        packetBufPtr[HEADER_LEN + NETWORK_ID_LEN + 1] = (byte)(&rbUsed);
                        packetBufPtr[HEADER_LEN + NETWORK_ID_LEN + 2] = (byte)(&rbGravity);

                        transform.id.CopyTo(packetBufPtr + HEADER_LEN);

                        fixed (float* transformBufPtr = &(transformBuf[0]))
                            UnsafeUtility.LongCopy((byte*)transformBufPtr, packetBufPtr + HEADER_LEN + (NETWORK_ID_LEN + BOOL_FIELD_LEN), RTC_TRANSFORM_LEN * sizeof(float));

                        var msgIdBuf = GetBytes(msgId);
                        fixed (byte* msgIdBufPtr = msgIdBuf)
                            UnsafeUtility.LongCopy(msgIdBufPtr, (packetBufPtr + sizeof(int)), msgIdBuf.Length);
                    }
                }

                return packetBuf;
            }

            public override void UnMarshall(byte[] bytes)
            {
                const int HEADER_LEN = 13;    // typ (1) + from (4) + to (4) + msgTyp (4)

                var transformBuf = new float[10];

                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    fixed (float* transformBufPtr = &(transformBuf[0]))
                    {
                        UnsafeUtility.LongCopy(bytesPtr + HEADER_LEN + (NETWORK_ID_LEN + BOOL_FIELD_LEN), (byte*)transformBufPtr, RTC_TRANSFORM_LEN * sizeof(float));

                        transform.id.Copy(bytesPtr + HEADER_LEN);

                        requested = *((bool*)&(bytesPtr[HEADER_LEN + NETWORK_ID_LEN + 0]));

                        transform.rb.Update(*((bool*)&(bytesPtr[HEADER_LEN + NETWORK_ID_LEN + 1])), *((bool*)&(bytesPtr[HEADER_LEN + NETWORK_ID_LEN + 2])));

                        transform.position.x = transformBuf[0];
                        transform.position.y = transformBuf[1];
                        transform.position.z = transformBuf[2];

                        transform.rotation.x = transformBuf[3];
                        transform.rotation.y = transformBuf[4];
                        transform.rotation.z = transformBuf[5];
                        transform.rotation.w = transformBuf[6];

                        transform.localScale.x = transformBuf[7];
                        transform.localScale.y = transformBuf[8];
                        transform.localScale.z = transformBuf[9];
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

        private MSG_SyncTransform m_tmp = new MSG_SyncTransform(new WebTransform());

        public bool enableGravity => (m_rb == null) ? false : m_rb.useGravity;

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
            if (m_rbHistory.Count < 3)  // ignore first element ...
            {
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
                return;
            }

            var history = m_rbHistory.ToArray();

            m_rbHistory.Clear();

            var positionDiff = Vector3.zero;
            var rotationDiff = Vector3.zero;

            for (int i = 2; i < history.Length; i++)
            {
                positionDiff += history[i].Item1 - history[i - 1].Item1;

                var tmp = Quaternion.Inverse(history[i - 1].Item2) * history[i].Item2;
                tmp.ToAngleAxis(out var angle, out var axis);

                rotationDiff += (m_rb.rotation * axis) * angle;
            }

            velocity = positionDiff / (history.Length - 1) / Time.deltaTime;
            angularVelocity = rotationDiff / (history.Length - 1) / Time.deltaTime;
        }

        public virtual void OnPhysicsBehaviourChange()
        {
            switch (NetworkClient.physicsBehaviour)
            {
                case NetworkClient.PhysicsBehaviour.Send:
                    EnableRigidbody(true);
                    break;
                case NetworkClient.PhysicsBehaviour.Recv:
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

        public override void OnSyncRequested(int from)
        {
            base.OnSyncRequested(from);

            SyncViaWebSocket(from, true, true);
        }

        public void SyncFrom(int from, WebTransform transform)
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

            m_rbState.Update(transform.rb.used, transform.rb.gravity);

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

        public override void SyncViaWebRTC(int to, bool force = false, bool requested = false)
        {
            if (!Const.Send.HasFlag(m_direction) || (m_state != State.Initialized))
                return;

            if (force || ApplyCurrentTransform())
            {
                m_tmp.transform = m_networkState;
                m_tmp.requested = requested;
                NetworkClient.instance.SendRTC(to, m_tmp.Marshall());

                m_synchronised = false;
            }
        }

        public override void SyncViaWebSocket(int to, bool force = false, bool requested = false)
        {
            if (!Const.Send.HasFlag(m_direction) || (m_state != State.Initialized))
                return;

            if (force || ApplyCurrentTransform())
            {
                m_tmp.transform = m_networkState;
                m_tmp.requested = requested;
                NetworkClient.instance.SendWS(to, m_tmp.Marshall());

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

                m_rbState.Update(true, m_rb.useGravity);

                EnableRigidbody(false, true);
            }
            else
                m_rbState.Update(false, false);
        }

        protected override void RegisterOnMessage()
        {
            base.RegisterOnMessage();

            NetworkClient.RegisterOnMessage<MSG_SyncTransform>((from, to, bytes) =>
            {
                m_tmp.UnMarshall(bytes);

                var transform = Registry.GetByKey(m_tmp.transform.id);
                if (transform)
                {
                    transform.SyncFrom(from, m_tmp.transform);
                    if (m_tmp.requested)
                        transform.OnSyncRequestCompleted(from);
                }
            });
        }

        protected override void Start()
        {
            base.Start();

            InitRigidbody();
        }

        protected override void Update()
        {
            UpdateRbHistory();

            SyncViaWebRTC(NetworkClient.userId);
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
