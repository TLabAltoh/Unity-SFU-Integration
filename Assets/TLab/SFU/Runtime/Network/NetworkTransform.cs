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
        public struct TransformState
        {
            public Address64 id;
            public RigidbodyState rb;
            public Vector3 position;
            public Vector4 rotation;
            public Vector3 localScale;

            public SerializableTransform ToSerializableTransform() => new SerializableTransform(position, rotation.ToQuaternion(), localScale);
        }

        #endregion STRUCT

        #region MESSAGE

        [Serializable, Message(typeof(MSG_SyncTransform))]
        public class MSG_SyncTransform : MSG_Sync
        {
            public TransformState transform;

            public const int BOOL_FIELD_LEN = 2;  // requested (1) + rbState.used (1) + rbState.gravity (1)

            public const int NETWORK_ID_LEN = 8;

            public const int RTC_TRANSFORM_LEN = 10;    // transform ((3 + 4 + 3) * 4)

            public const int PAYLOAD_LEN = NETWORK_ID_LEN + BOOL_FIELD_LEN + RTC_TRANSFORM_LEN * sizeof(float);

            private byte[] m_packetBuf = new byte[SEND_HEADER_LEN + PAYLOAD_LEN];

            private float[] m_transformBuf = new float[RTC_TRANSFORM_LEN];

            private const int SEND_HEADER_LEN = 8;    // to (4) + msgTyp (4)

            private const int RECV_HEADER_LEN = 13;    // typ (1) + from (4) + to (4) + msgTyp (4)

            public MSG_SyncTransform(TransformState transform) : base()
            {
                this.transform = transform;
            }

            public MSG_SyncTransform(byte[] bytes) : base(bytes) { }

            public override byte[] Marshall()
            {
                m_transformBuf[0] = transform.position.x;
                m_transformBuf[1] = transform.position.y;
                m_transformBuf[2] = transform.position.z;

                m_transformBuf[3] = transform.rotation.x;
                m_transformBuf[4] = transform.rotation.y;
                m_transformBuf[5] = transform.rotation.z;
                m_transformBuf[6] = transform.rotation.w;

                m_transformBuf[7] = transform.localScale.x;
                m_transformBuf[8] = transform.localScale.y;
                m_transformBuf[9] = transform.localScale.z;

                unsafe
                {
                    fixed (byte* packetBufPtr = m_packetBuf)
                    {
                        bool rbUsed = transform.rb.used, rbGravity = transform.rb.gravity, requested = this.requested;

                        packetBufPtr[SEND_HEADER_LEN + NETWORK_ID_LEN + 0] = *((byte*)(&requested));
                        packetBufPtr[SEND_HEADER_LEN + NETWORK_ID_LEN + 1] = *((byte*)(&rbUsed));
                        packetBufPtr[SEND_HEADER_LEN + NETWORK_ID_LEN + 2] = *((byte*)(&rbGravity));

                        transform.id.CopyTo(packetBufPtr + SEND_HEADER_LEN);

                        fixed (float* transformBufPtr = &(m_transformBuf[0]))
                            UnsafeUtility.LongCopy((byte*)transformBufPtr, packetBufPtr + SEND_HEADER_LEN + (NETWORK_ID_LEN + BOOL_FIELD_LEN), RTC_TRANSFORM_LEN * sizeof(float));

                        var msgIdBuf = GetBytes(msgId);
                        fixed (byte* msgIdBufPtr = msgIdBuf)
                            UnsafeUtility.LongCopy(msgIdBufPtr, (packetBufPtr + sizeof(int)), msgIdBuf.Length);
                    }
                }

                return m_packetBuf;
            }

            public override void UnMarshall(byte[] bytes)
            {
                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    fixed (float* m_transformBufPtr = &(m_transformBuf[0]))
                    {
                        UnsafeUtility.LongCopy(bytesPtr + RECV_HEADER_LEN + (NETWORK_ID_LEN + BOOL_FIELD_LEN), (byte*)m_transformBufPtr, RTC_TRANSFORM_LEN * sizeof(float));

                        transform.id.Copy(bytesPtr + RECV_HEADER_LEN);

                        requested = *((bool*)&(bytesPtr[RECV_HEADER_LEN + NETWORK_ID_LEN + 0]));

                        transform.rb.Update(*((bool*)&(bytesPtr[RECV_HEADER_LEN + NETWORK_ID_LEN + 1])), *((bool*)&(bytesPtr[RECV_HEADER_LEN + NETWORK_ID_LEN + 2])));

                        transform.position.x = m_transformBuf[0];
                        transform.position.y = m_transformBuf[1];
                        transform.position.z = m_transformBuf[2];

                        transform.rotation.x = m_transformBuf[3];
                        transform.rotation.y = m_transformBuf[4];
                        transform.rotation.z = m_transformBuf[5];
                        transform.rotation.w = m_transformBuf[6];

                        transform.localScale.x = m_transformBuf[7];
                        transform.localScale.y = m_transformBuf[8];
                        transform.localScale.z = m_transformBuf[9];
                    }
                }
            }
        }

        #endregion MESSAGE

        public enum InterpolationMode
        {
            None,
            Step,
        };

        [SerializeField] protected NetworkTransform m_parent;

        [Header("Transform Threshold")]

        public float positionThreshold = 0.001f;
        [Range(0.00001f, 360.0f)] public float rotAngleThreshold = 0.01f;
        public float scaleThreshold = 0.01f;

        [Header("Interpolation")]

        [SerializeField] protected InterpolationMode m_interpolationMode = InterpolationMode.None;
        [SerializeField, Min(1)] protected int m_interpolationStep = 2;

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

        protected TransformState m_interpolationStart;
        protected TransformState m_interpolationTarget;
        protected TransformState m_delta;
        protected TransformState m_networkState;

        protected int m_interpolation = -1;

        public InterpolationMode interpolationMode => m_interpolationMode;

        public int interpolationStep => m_interpolationStep;

        public Rigidbody rb => m_rb;

        public RigidbodyState rbState => m_rbState;

        private MSG_SyncTransform m_tmp = new MSG_SyncTransform(new TransformState());

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

        public virtual void OnRigidbodyModeChange()
        {
            switch (NetworkClient.rbMode)
            {
                case NetworkClient.RigidbodyMode.Send:
                    EnableRigidbody(true);
                    break;
                case NetworkClient.RigidbodyMode.Recv:
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

        protected void UpdateTransform(in Vector3 position, in Quaternion rotation, in Vector3 localScale)
        {
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
        }

        protected void UpdateTransform(in TransformState target) => UpdateTransform(target.position, target.rotation.ToQuaternion(), target.localScale);

        protected virtual void GetInterpolatedTransform(in SerializableTransform start, in SerializableTransform target, out Vector3 position, out Quaternion rotation, out Vector3 localScale, float t)
        {
            position = Vector3.Lerp(start.position, target.position, t);
            rotation = start.rotation.LerpQuaternion(target.rotation, t);
            localScale = Vector3.Lerp(start.localScale, target.localScale, t);
        }

        protected virtual void InterpolateTransform()
        {
            switch (m_interpolationMode)
            {
                case InterpolationMode.None:
                    break;
                case InterpolationMode.Step:
                    if (m_interpolation > 0)
                        m_interpolation--;

                    if (m_interpolation == 0)
                    {
                        UpdateTransform(m_interpolationTarget);
                        StopInterpolation();
                        return;
                    }

                    if (m_interpolation > 0)
                    {
                        var t = (float)(m_interpolationStep - m_interpolation) / m_interpolationStep;

                        GetInterpolatedTransform(m_interpolationStart.ToSerializableTransform(), m_interpolationTarget.ToSerializableTransform(), out var position, out var rotation, out var localScale, t);

                        UpdateTransform(position, rotation, localScale);

                        return;
                    }
                    break;
            }
        }

        protected virtual void StartInterpolation(in Vector3 position, in Quaternion rotation, in Vector3 localScale)
        {
            m_interpolationStart.position = this.transform.position;
            m_interpolationStart.rotation = this.transform.rotation.ToVec();
            m_interpolationStart.localScale = this.transform.localScale;

            m_interpolationTarget.position = position;
            m_interpolationTarget.rotation = rotation.ToVec();
            m_interpolationTarget.localScale = localScale;

            m_interpolation = m_interpolationStep;
        }

        protected virtual void StopInterpolation()
        {
            ApplyTransform(this.transform.position, this.transform.rotation, this.transform.localScale);
            m_interpolation = -1;
        }

        public void SyncFrom(in int from, in TransformState transform)
        {
            Vector3 position;
            Vector4 rotation;

            if (m_parent != null)
            {
                position = m_parent.transform.TransformPoint(transform.position);
                rotation = (transform.rotation.ToQuaternion() * m_parent.transform.rotation).ToVec();
            }
            else
            {
                position = transform.position;
                rotation = transform.rotation;
            }

            var localScale = transform.localScale;

            switch (m_interpolationMode)
            {
                case InterpolationMode.None:
                    UpdateTransform(position, rotation.ToQuaternion(), localScale);
                    break;
                case InterpolationMode.Step:
                    StartInterpolation(position, rotation.ToQuaternion(), localScale);
                    break;
            }

            m_rbState.Update(transform.rb.used, transform.rb.gravity);

            UpdateRbHistory();

            ApplyTransform(position, rotation, localScale);

            m_synchronised = true;
        }

        protected virtual void ApplyTransform(in Vector3 position, in Quaternion rotation, in Vector3 localScale)
        {
            if (m_parent != null)
            {
                var positionDelta = m_parent.transform.InverseTransformPoint(position);
                var rotationDelta = m_parent.transform.rotation * Quaternion.Inverse(rotation);

                ApplyTransformPosition(position);
                ApplyTransformRotation(rotation);
                ApplyTransformPositionDelta(positionDelta);
                ApplyTransformRotationDelta(rotationDelta);
            }
            else
            {
                ApplyTransformPosition(position);
                ApplyTransformRotation(rotation);
            }

            ApplyTransformLocalScale(localScale);
        }

        protected virtual void ApplyTransform(in Vector3 position, in Vector4 rotation, in Vector3 localScale) => ApplyTransform(position, rotation.ToQuaternion(), localScale);

        protected virtual void ApplyTransform(in Transform transform) => ApplyTransform(transform.position, transform.rotation, transform.localScale);

        protected virtual void ApplyTransformPosition(in Vector3 position)
        {
            m_networkState.position.x = position.x;
            m_networkState.position.y = position.y;
            m_networkState.position.z = position.z;
        }

        protected virtual void ApplyTransformRotation(in Vector4 rotation)
        {
            m_networkState.rotation.x = rotation.x;
            m_networkState.rotation.y = rotation.y;
            m_networkState.rotation.z = rotation.z;
            m_networkState.rotation.w = rotation.w;
        }

        protected virtual void ApplyTransformRotation(in Quaternion rotation)
        {
            m_networkState.rotation.x = rotation.x;
            m_networkState.rotation.y = rotation.y;
            m_networkState.rotation.z = rotation.z;
            m_networkState.rotation.w = rotation.w;
        }

        protected virtual void ApplyTransformLocalScale(in Vector3 localScale)
        {
            m_delta.localScale.x = localScale.x;
            m_delta.localScale.y = localScale.y;
            m_delta.localScale.z = localScale.z;

            m_networkState.localScale.x = localScale.x;
            m_networkState.localScale.y = localScale.y;
            m_networkState.localScale.z = localScale.z;
        }

        protected virtual void ApplyTransformPositionDelta(in Vector3 positionDelta)
        {
            m_delta.position.x = positionDelta.x;
            m_delta.position.y = positionDelta.y;
            m_delta.position.z = positionDelta.z;
        }

        protected virtual void ApplyTransformRotationDelta(in Vector4 rotationDelta)
        {
            m_delta.rotation.x = rotationDelta.x;
            m_delta.rotation.y = rotationDelta.y;
            m_delta.rotation.z = rotationDelta.z;
            m_delta.rotation.w = rotationDelta.w;
        }

        protected virtual void ApplyTransformRotationDelta(in Quaternion rotationDelta)
        {
            m_delta.rotation.x = rotationDelta.x;
            m_delta.rotation.y = rotationDelta.y;
            m_delta.rotation.z = rotationDelta.z;
            m_delta.rotation.w = rotationDelta.w;
        }

        public virtual bool ApplyCurrentTransform(out Vector3 position, out Vector4 rotation, out Vector3 localScale)
        {
            bool isDirty = false;

            m_networkState.id = m_networkId.id;

            if (m_networkState.rb != m_rbState)
            {
                m_networkState.rb = m_rbState;
                isDirty = true;
            }

            if (m_parent != null)
            {
                var positionDelta = m_parent.transform.InverseTransformPoint(this.transform.position);
                var rotationDelta = m_parent.transform.rotation * Quaternion.Inverse(this.transform.rotation);

                position = m_delta.position;
                rotation = m_delta.rotation;

                if (Vector3.Distance(m_delta.position, positionDelta) > positionThreshold)
                {
                    ApplyTransformPosition(transform.position);
                    ApplyTransformPositionDelta(positionDelta);

                    position = positionDelta;

                    isDirty = true;
                }

                if (Quaternion.Angle(m_delta.rotation.ToQuaternion(), rotationDelta) > rotAngleThreshold)
                {
                    ApplyTransformRotation(transform.rotation);
                    ApplyTransformRotationDelta(rotationDelta);

                    rotation = rotationDelta.ToVec();

                    isDirty = true;
                }
            }
            else
            {
                position = m_networkState.position;
                rotation = m_networkState.rotation;

                if (Vector3.Distance(m_networkState.position, this.transform.position) > positionThreshold)
                {
                    ApplyTransformPosition(this.transform.position);

                    position = this.transform.position;

                    isDirty = true;
                }

                if (Quaternion.Angle(m_networkState.rotation.ToQuaternion(), this.transform.rotation) > rotAngleThreshold)
                {
                    ApplyTransformRotation(this.transform.rotation);

                    rotation = this.transform.rotation.ToVec();

                    isDirty = true;
                }
            }

            localScale = m_networkState.localScale;

            if (Vector3.Distance(m_networkState.localScale, this.transform.localScale) > scaleThreshold)
            {
                ApplyTransformLocalScale(this.transform.localScale);

                localScale = this.transform.localScale;

                isDirty = true;
            }

            return isDirty;
        }

        public override void SyncViaWebRTC(int to, bool force = false, bool requested = false)
        {
            if (!Const.Send.HasFlag(m_direction) || (m_state != State.Initialized) || (m_interpolation > 0))
                return;

            if (ApplyCurrentTransform(out var position, out var rotation, out var localScale) || force)
            {
                m_tmp.transform.id = m_networkState.id;
                m_tmp.transform.rb = m_networkState.rb;
                m_tmp.transform.position = position;
                m_tmp.transform.rotation = rotation;
                m_tmp.transform.localScale = localScale;

                m_tmp.requested = requested;

                NetworkClient.instance.SendRTC(to, m_tmp.Marshall());

                m_synchronised = false;
            }
        }

        public override void SyncViaWebSocket(int to, bool force = false, bool requested = false)
        {
            if (!Const.Send.HasFlag(m_direction) || (m_state != State.Initialized) || (m_interpolation > 0))
                return;

            if (ApplyCurrentTransform(out var position, out var rotation, out var localScale) || force)
            {
                m_tmp.transform.id = m_networkState.id;
                m_tmp.transform.rb = m_networkState.rb;
                m_tmp.transform.position = position;
                m_tmp.transform.rotation = rotation;
                m_tmp.transform.localScale = localScale;

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

            ApplyTransform(this.transform);

            InitRigidbody();
        }

        protected override void Update()
        {
            InterpolateTransform();

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
