using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using static TLab.SFU.UnsafeUtility;

namespace TLab.SFU.Network
{
    using Registry = Registry<Address64, NetworkRigidbodyTransform>;

    [AddComponentMenu("TLab/SFU/Network Rigidbody Transform (TLab)")]
    public class NetworkRigidbodyTransform : NetworkObject
    {
        #region STRUCT

        [Serializable]
        public struct NetworkRigidbodyTransformState
        {
            public Address64 id;

            public RigidbodyState rigidbody;
            public SerializableTransform transform;
        }

        #endregion STRUCT

        #region MESSAGE

        [Serializable, Message(typeof(MSG_SyncRigidbodyTransform))]
        public class MSG_SyncRigidbodyTransform : MSG_Sync
        {
            #region CONSTANT

            private const int SEND_HEADER_LEN = 8, RECV_HEADER_LEN = 13;    // SEND: to (4) + msgId (4), RECV: typ (1) + from (4) + to (4) + msgId (4)

            private const int NETWORK_ID_FIELD_LEN = 8;

            private const int BOOL_FIELD_OFFSET = 0, BOOL_FIELD_LEN = 4;  // request (1) + immediate (1) + rbState.active (1) + rbState.gravity (1)

            private const int TRANSFORM_FIELD_OFFSET = BOOL_FIELD_OFFSET + BOOL_FIELD_LEN, TRANSFORM_FIELD_LEN = 10;    // state ((3 + 4 + 3) * 4)

            private const int PAYLOAD_LEN = NETWORK_ID_FIELD_LEN + BOOL_FIELD_LEN + TRANSFORM_FIELD_LEN * sizeof(float);

            #endregion CONSTANT

            public NetworkRigidbodyTransformState state;

            private static byte[] m_packetBuf = new byte[SEND_HEADER_LEN + PAYLOAD_LEN];

            private static float[] m_transformBuf = new float[TRANSFORM_FIELD_LEN];

            public MSG_SyncRigidbodyTransform(NetworkRigidbodyTransformState state) : base()
            {
                this.state = state;
            }

            public MSG_SyncRigidbodyTransform(byte[] bytes) : base(bytes) { }

            public override byte[] Marshall()
            {
                m_transformBuf[0] = state.transform.position.x;
                m_transformBuf[1] = state.transform.position.y;
                m_transformBuf[2] = state.transform.position.z;

                m_transformBuf[3] = state.transform.rotation.x;
                m_transformBuf[4] = state.transform.rotation.y;
                m_transformBuf[5] = state.transform.rotation.z;
                m_transformBuf[6] = state.transform.rotation.w;

                m_transformBuf[7] = state.transform.localScale.x;
                m_transformBuf[8] = state.transform.localScale.y;
                m_transformBuf[9] = state.transform.localScale.z;

                unsafe
                {
                    fixed (byte* m_packetBufPtr = m_packetBuf)
                    {
                        Copy(msgId, m_packetBufPtr + sizeof(int));

                        var payloadPtr = m_packetBufPtr + SEND_HEADER_LEN;

                        state.id.CopyTo(payloadPtr);

                        Copy(request, payloadPtr + BOOL_FIELD_OFFSET + 0);
                        Copy(immediate, payloadPtr + BOOL_FIELD_OFFSET + 1);
                        Copy(state.rigidbody.used, payloadPtr + BOOL_FIELD_OFFSET + 2);
                        Copy(state.rigidbody.gravity, payloadPtr + BOOL_FIELD_OFFSET + 3);

                        Copy(m_transformBuf, payloadPtr + TRANSFORM_FIELD_OFFSET, TRANSFORM_FIELD_LEN);
                    }
                }

                return m_packetBuf;
            }

            public override void UnMarshall(byte[] bytes)
            {
                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    {
                        var payloadPtr = bytesPtr + RECV_HEADER_LEN;

                        state.id.Copy(payloadPtr);

                        request = Get(payloadPtr + BOOL_FIELD_OFFSET + 0);
                        immediate = Get(payloadPtr + BOOL_FIELD_OFFSET + 1);

                        state.rigidbody.Update(Get(payloadPtr + BOOL_FIELD_OFFSET + 2), Get(payloadPtr + BOOL_FIELD_OFFSET + 3));

                        Copy(payloadPtr + TRANSFORM_FIELD_OFFSET, m_transformBuf, TRANSFORM_FIELD_LEN * sizeof(float));

                        state.transform.position.x = m_transformBuf[0];
                        state.transform.position.y = m_transformBuf[1];
                        state.transform.position.z = m_transformBuf[2];

                        state.transform.rotation.x = m_transformBuf[3];
                        state.transform.rotation.y = m_transformBuf[4];
                        state.transform.rotation.z = m_transformBuf[5];
                        state.transform.rotation.w = m_transformBuf[6];

                        state.transform.localScale.x = m_transformBuf[7];
                        state.transform.localScale.y = m_transformBuf[8];
                        state.transform.localScale.z = m_transformBuf[9];
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

        [SerializeField] protected NetworkRigidbodyTransform m_parent;

        [Header("Transform Threshold")]

        public float positionThreshold = 0.001f;
        [Range(0.00001f, 360.0f)] public float rotAngleThreshold = 0.01f;
        public float scaleThreshold = 0.01f;

        [Header("Attenuation")]
        [Range(0f, 1f)] public float velocityAttenuation = 0.85f;
        [Range(0f, 1f)] public float angularVelocityAttenuation = 0.85f;

        [Header("Interpolation")]

        [SerializeField] protected InterpolationMode m_interpolationMode = InterpolationMode.None;
        [SerializeField, Min(1)] protected int m_interpolationStep = 3;

        protected Rigidbody m_rb;

        protected RigidbodyInterpolation m_rbInterpolation;

        protected RigidbodyState m_rbState;

        private SerializableTransform m_prev;

        protected NetworkRigidbodyTransformState m_networkState;

        protected SerializableTransform m_delta;

        public static readonly float INTERPOLATION_BASE_FPS = 30;

        protected struct InterpolationState
        {
            public int current;
            public int step;

            public SerializableTransform start;
            public SerializableTransform target;

            public InterpolationState(int dummy = 0)
            {
                this.current = -1;
                this.step = -1;

                this.start = new SerializableTransform();
                this.target = new SerializableTransform();
            }
        }

        protected InterpolationState m_interpolationState = new InterpolationState(0);

        public InterpolationMode interpolationMode => m_interpolationMode;

        public int interpolationStep => m_interpolationStep;

        public Rigidbody rb => m_rb;

        public RigidbodyState rbState => m_rbState;

        private static MSG_SyncRigidbodyTransform m_packet = new MSG_SyncRigidbodyTransform(new NetworkRigidbodyTransformState());

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

#if UNITY_EDITOR
        public virtual void UseRigidbody(bool rigidbody, bool gravity)
        {
            if (EditorApplication.isPlaying)
                return;

            var rb = gameObject.RequireComponent<Rigidbody>();
            rb.useGravity = gravity;
        }
#endif

        public virtual void OnRigidbodyModeChange()
        {
            switch (NetworkClient.rbMode)
            {
                case NetworkClient.RigidbodyMode.Send:
                    if (m_rb != null)
                    {
                        m_rb.isKinematic = false;
                        m_rb.interpolation = m_rbInterpolation;
                    }

                    EnableRigidbody(true);
                    break;
                case NetworkClient.RigidbodyMode.Recv:
                    if (m_rb != null)
                    {
                        m_rb.isKinematic = true;
                        m_rb.interpolation = RigidbodyInterpolation.None;
                    }

                    EnableRigidbody(false, true);
                    break;
            }
        }

        public virtual void EnableRigidbody(bool enable, bool force = false)
        {
            if (!m_rbState.used)
                return;

            if (enable && m_rbState.gravity)
            {
                m_rb.useGravity = true;

                var deltaRot = transform.rotation * Quaternion.Inverse(m_prev.rotation.ToQuaternion());
                deltaRot.ToAngleAxis(out var magnitude, out var axis);

                magnitude *= Mathf.Deg2Rad;

                m_rb.angularVelocity = angularVelocityAttenuation * (1.0f / Time.deltaTime) * magnitude * axis;

                m_rb.velocity = velocityAttenuation * (transform.position - m_prev.position) / Time.deltaTime;
            }
            else
            {
                m_rb.useGravity = false;

                m_rb.velocity = Vector3.zero;
                m_rb.angularVelocity = Vector3.zero;
            }
        }

        public override void OnSyncRequest(int from)
        {
            base.OnSyncRequest(from);

            SyncViaWebSocket(from, true, true, true);
        }

        protected void UpdateTransform(in Vector3 position, in Quaternion rotation, in Vector3 localScale)
        {
            this.transform.localScale = new Vector3(localScale.x, localScale.y, localScale.z);
            this.transform.position = new Vector3(position.x, position.y, position.z);
            this.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        }

        protected void UpdateTransform(in SerializableTransform target) => UpdateTransform(target.position, target.rotation.ToQuaternion(), target.localScale);

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
                    if (m_interpolationState.current > 0)
                        m_interpolationState.current--;

                    if (m_interpolationState.current == 0)
                    {
                        UpdateTransform(m_interpolationState.target);
                        StopInterpolation();
                        return;
                    }

                    if (m_interpolationState.current > 0)
                    {
                        var t = (float)(m_interpolationState.step - m_interpolationState.current) / m_interpolationState.step;

                        GetInterpolatedTransform(m_interpolationState.start, m_interpolationState.target, out var position, out var rotation, out var localScale, t);

                        UpdateTransform(position, rotation, localScale);

                        return;
                    }
                    break;
            }
        }

        protected virtual void StartInterpolation(in Vector3 position, in Quaternion rotation, in Vector3 localScale)
        {
            m_interpolationState.start.position = this.transform.position;
            m_interpolationState.start.rotation = this.transform.rotation.ToVec();
            m_interpolationState.start.localScale = this.transform.localScale;

            m_interpolationState.target.position = position;
            m_interpolationState.target.rotation = rotation.ToVec();
            m_interpolationState.target.localScale = localScale;

            m_interpolationState.step = Math.Max(1, m_interpolationStep * (int)((1 / Time.deltaTime) / INTERPOLATION_BASE_FPS));
            m_interpolationState.current = m_interpolationState.step;
        }

        protected virtual void StopInterpolation()
        {
            ApplyTransform(this.transform.position, this.transform.rotation, this.transform.localScale);
            m_interpolationState.current = -1;
        }

        public void SyncFrom(in int from, in bool immediate, in NetworkRigidbodyTransformState state)
        {
            Vector3 position;
            Vector4 rotation;

            if (m_parent != null)
            {
                position = m_parent.transform.TransformPoint(state.transform.position);
                rotation = (state.transform.rotation.ToQuaternion() * m_parent.transform.rotation).ToVec();
            }
            else
            {
                position = state.transform.position;
                rotation = state.transform.rotation;
            }

            var localScale = state.transform.localScale;

            if (immediate)
                UpdateTransform(position, rotation.ToQuaternion(), localScale);
            else
            {
                switch (m_interpolationMode)
                {
                    case InterpolationMode.None:
                        UpdateTransform(position, rotation.ToQuaternion(), localScale);
                        break;
                    case InterpolationMode.Step:
                        StartInterpolation(position, rotation.ToQuaternion(), localScale);
                        break;
                }
            }

            m_rbState.Update(state.rigidbody.used, state.rigidbody.gravity);

            ApplyTransform(position, rotation, localScale);

            m_synchronised = true;
        }

        protected virtual void ApplyTransform(in Vector3 position, in Quaternion rotation, in Vector3 localScale)
        {
            if (m_parent != null)
            {
                var positionDelta = m_parent.transform.InverseTransformPoint(position);
                var rotationDelta = rotation * Quaternion.Inverse(m_parent.transform.rotation);

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

        protected virtual void ApplyTransform(in Transform state) => ApplyTransform(state.position, state.rotation, state.localScale);

        protected virtual void ApplyTransformPosition(in Vector3 position)
        {
            m_networkState.transform.position.x = position.x;
            m_networkState.transform.position.y = position.y;
            m_networkState.transform.position.z = position.z;
        }

        protected virtual void ApplyTransformRotation(in Vector4 rotation)
        {
            m_networkState.transform.rotation.x = rotation.x;
            m_networkState.transform.rotation.y = rotation.y;
            m_networkState.transform.rotation.z = rotation.z;
            m_networkState.transform.rotation.w = rotation.w;
        }

        protected virtual void ApplyTransformRotation(in Quaternion rotation)
        {
            m_networkState.transform.rotation.x = rotation.x;
            m_networkState.transform.rotation.y = rotation.y;
            m_networkState.transform.rotation.z = rotation.z;
            m_networkState.transform.rotation.w = rotation.w;
        }

        protected virtual void ApplyTransformLocalScale(in Vector3 localScale)
        {
            m_delta.localScale.x = localScale.x;
            m_delta.localScale.y = localScale.y;
            m_delta.localScale.z = localScale.z;

            m_networkState.transform.localScale.x = localScale.x;
            m_networkState.transform.localScale.y = localScale.y;
            m_networkState.transform.localScale.z = localScale.z;
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

            if (m_networkState.rigidbody != m_rbState)
            {
                m_networkState.rigidbody = m_rbState;
                isDirty = true;
            }

            if (m_parent != null)
            {
                var positionDelta = m_parent.transform.InverseTransformPoint(this.transform.position);
                var rotationDelta = this.transform.rotation * Quaternion.Inverse(m_parent.transform.rotation);

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
                position = m_networkState.transform.position;
                rotation = m_networkState.transform.rotation;

                if (Vector3.Distance(m_networkState.transform.position, this.transform.position) > positionThreshold)
                {
                    ApplyTransformPosition(this.transform.position);

                    position = this.transform.position;

                    isDirty = true;
                }

                if (Quaternion.Angle(m_networkState.transform.rotation.ToQuaternion(), this.transform.rotation) > rotAngleThreshold)
                {
                    ApplyTransformRotation(this.transform.rotation);

                    rotation = this.transform.rotation.ToVec();

                    isDirty = true;
                }
            }

            localScale = m_networkState.transform.localScale;

            if (Vector3.Distance(m_networkState.transform.localScale, this.transform.localScale) > scaleThreshold)
            {
                ApplyTransformLocalScale(this.transform.localScale);

                localScale = this.transform.localScale;

                isDirty = true;
            }

            return isDirty;
        }

        public virtual bool SkipApplyCurrentTransform() => !Const.Send.HasFlag(m_direction) || !initialized || (m_interpolationState.current > 0);

        private void SetSendPacket(in Vector3 position, in Vector4 rotation, in Vector3 localScale, bool request = false, bool immediate = false)
        {
            m_packet.state.id = m_networkState.id;
            m_packet.state.rigidbody = m_networkState.rigidbody;

            m_packet.state.transform.position = position;
            m_packet.state.transform.rotation = rotation;
            m_packet.state.transform.localScale = localScale;

            m_packet.request = request;
            m_packet.immediate = immediate;
        }

        private void SendRTC(int to, in Vector3 position, in Vector4 rotation, in Vector3 localScale, bool request = false, bool immediate = false)
        {
            SetSendPacket(position, rotation, localScale, request, immediate);
            NetworkClient.SendRTC(to, m_packet.Marshall());

            m_synchronised = false;
        }

        private void SendWS(int to, in Vector3 position, in Vector4 rotation, in Vector3 localScale, bool request = false, bool immediate = false)
        {
            SetSendPacket(position, rotation, localScale, request, immediate);
            NetworkClient.SendWS(to, m_packet.Marshall());

            m_synchronised = false;
        }

        public override void SyncViaWebRTC(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (SkipApplyCurrentTransform())
                return;

            if (ApplyCurrentTransform(out var position, out var rotation, out var localScale) || force)
                SendRTC(to, position, rotation, localScale, force, request);
        }

        public override void SyncViaWebSocket(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (SkipApplyCurrentTransform())
                return;

            if (ApplyCurrentTransform(out var position, out var rotation, out var localScale) || force)
                SendWS(to, position, rotation, localScale, force, request);
        }

        protected virtual void InitRigidbody()
        {
            m_rb = GetComponent<Rigidbody>();

            if (m_rb != null)
            {
                m_rbState.Update(true, m_rb.useGravity);

                m_rbInterpolation = m_rb.interpolation;

                EnableRigidbody(false, true);
            }
            else
                m_rbState.Update(false, false);
        }

        protected override void RegisterOnMessage()
        {
            base.RegisterOnMessage();

            NetworkClient.RegisterOnMessage<MSG_SyncRigidbodyTransform>((from, to, bytes) =>
            {
                m_packet.UnMarshall(bytes);

                var state = Registry.GetByKey(m_packet.state.id);
                if (state)
                {
                    state.SyncFrom(from, m_packet.immediate, m_packet.state);
                    if (m_packet.request)
                        state.OnSyncRequestComplete(from);
                }
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            CachePrevPosAndRot();
        }

        protected override void Start()
        {
            base.Start();

            ApplyTransform(this.transform);

            InitRigidbody();
        }

        protected virtual void CachePrevPosAndRot()
        {
            m_prev.position = transform.position;
            m_prev.rotation = transform.rotation.ToVec();
        }

        protected override void Update()
        {
            InterpolateTransform();

            CachePrevPosAndRot();

            Sync(NetworkClient.userId);
        }

        protected override void Register()
        {
            base.Register();

            Registry.Register(m_networkId.id, this);
        }

        protected override void Unregister()
        {
            Registry.Unregister(m_networkId.id);

            base.Unregister();
        }
    }
}
