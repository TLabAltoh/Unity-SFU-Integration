using System;
using UnityEngine;
using Unity.Mathematics;

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

            private const int BOOL_FIELD_OFFSET = NETWORK_ID_FIELD_LEN, BOOL_FIELD_LEN = 4;  // request (1) + immediate (1) + rbState.active (1) + rbState.gravity (1)

            private const int TRANSFORM_FIELD_OFFSET = BOOL_FIELD_OFFSET + BOOL_FIELD_LEN, TRANSFORM_FIELD_LEN = 10;    // ((3 + 4 + 3) * 4)

#if TLAB_SFU_USE_HALF_FLOAT
            private const int TRANSFORM_FIELD_VALUE_SIZE = 2;   // 16 bit
#else
            private const int TRANSFORM_FIELD_VALUE_SIZE = sizeof(float);
#endif

            private const int PAYLOAD_LEN = NETWORK_ID_FIELD_LEN + BOOL_FIELD_LEN + TRANSFORM_FIELD_LEN * TRANSFORM_FIELD_VALUE_SIZE;

            #endregion CONSTANT

            public NetworkRigidbodyTransformState state;

            private static byte[] m_packet = new byte[SEND_HEADER_LEN + PAYLOAD_LEN];

#if TLAB_SFU_USE_HALF_FLOAT
            private static half[] m_transform = new half[TRANSFORM_FIELD_LEN];
#else
            private static float[] m_transform = new float[TRANSFORM_FIELD_LEN];
#endif

            public MSG_SyncRigidbodyTransform(NetworkRigidbodyTransformState state) : base()
            {
                this.state = state;
            }

            public MSG_SyncRigidbodyTransform(byte[] bytes) : base(bytes) { }

            public override byte[] Marshall()
            {
#if TLAB_SFU_USE_HALF_FLOAT
                m_transform[0] = math.half(state.transform.position.x);
                m_transform[1] = math.half(state.transform.position.y);
                m_transform[2] = math.half(state.transform.position.z);

                m_transform[3] = math.half(state.transform.rotation.x);
                m_transform[4] = math.half(state.transform.rotation.y);
                m_transform[5] = math.half(state.transform.rotation.z);
                m_transform[6] = math.half(state.transform.rotation.w);

                m_transform[7] = math.half(state.transform.localScale.x);
                m_transform[8] = math.half(state.transform.localScale.y);
                m_transform[9] = math.half(state.transform.localScale.z);
#else
                m_transform[0] = state.transform.position.x;
                m_transform[1] = state.transform.position.y;
                m_transform[2] = state.transform.position.z;

                m_transform[3] = state.transform.rotation.x;
                m_transform[4] = state.transform.rotation.y;
                m_transform[5] = state.transform.rotation.z;
                m_transform[6] = state.transform.rotation.w;

                m_transform[7] = state.transform.localScale.x;
                m_transform[8] = state.transform.localScale.y;
                m_transform[9] = state.transform.localScale.z;
#endif

                unsafe
                {
                    fixed (byte* s = m_packet)
                    {
                        Copy(msgId, s + sizeof(int));

                        var p = s + SEND_HEADER_LEN;

                        state.id.CopyTo(p);

                        var b = p + BOOL_FIELD_OFFSET;
                        Copy(request, b + 0);
                        Copy(immediate, b + 1);
                        Copy(state.rigidbody.used, b + 2);
                        Copy(state.rigidbody.gravity, b + 3);

                        Copy(m_transform, p + TRANSFORM_FIELD_OFFSET, TRANSFORM_FIELD_LEN);
                    }
                }

                return m_packet;
            }

            public override void UnMarshall(byte[] bytes)
            {
                unsafe
                {
                    fixed (byte* s = bytes)
                    {
                        var p = s + RECV_HEADER_LEN;

                        state.id.Copy(p);

                        var b = p + BOOL_FIELD_OFFSET;
                        request = Get(b + 0);
                        immediate = Get(b + 1);
                        state.rigidbody.Update(Get(b + 2), Get(b + 3));

                        Copy(p + TRANSFORM_FIELD_OFFSET, m_transform, TRANSFORM_FIELD_LEN * TRANSFORM_FIELD_VALUE_SIZE);

                        state.transform.position.x = m_transform[0];
                        state.transform.position.y = m_transform[1];
                        state.transform.position.z = m_transform[2];

                        state.transform.rotation.x = m_transform[3];
                        state.transform.rotation.y = m_transform[4];
                        state.transform.rotation.z = m_transform[5];
                        state.transform.rotation.w = m_transform[6];

                        state.transform.localScale.x = m_transform[7];
                        state.transform.localScale.y = m_transform[8];
                        state.transform.localScale.z = m_transform[9];
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

        protected SerializableTransform m_prev;

        protected NetworkRigidbodyTransformState m_networkState;

        public static readonly float INTERPOLATION_BASE_FPS = 30;

        protected struct InterpolationHandler
        {
            public int current;
            public int step;

            public SerializableTransform start;
            public SerializableTransform target;

            public InterpolationHandler(int dummy = 0)
            {
                this.current = -1;
                this.step = -1;

                this.start = new SerializableTransform();
                this.target = new SerializableTransform();
            }
        }

        protected InterpolationHandler m_handler = new InterpolationHandler(0);

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

                var deltaRot = Quaternion.Inverse(m_prev.rotation.ToQuaternion()) * transform.rotation;
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
            if (m_parent != null)
            {
                this.transform.position = m_parent.transform.TransformPoint(position);
                this.transform.rotation = m_parent.transform.rotation * rotation;
            }
            else
            {
                this.transform.position = new(position.x, position.y, position.z);
                this.transform.rotation = new(rotation.x, rotation.y, rotation.z, rotation.w);
            }
            this.transform.localScale = new(localScale.x, localScale.y, localScale.z);
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
            if (m_handler.current > 0)
                m_handler.current--;

            if (m_handler.current == 0)
            {
                UpdateTransform(m_handler.target);
                StopInterpolation();
                return;
            }

            switch (m_interpolationMode)
            {
                case InterpolationMode.None:
                    break;
                case InterpolationMode.Step:
                    if (m_handler.current > 0)
                    {
                        var t = (float)(m_handler.step - m_handler.current) / m_handler.step;

                        GetInterpolatedTransform(m_handler.start, m_handler.target, out var position, out var rotation, out var localScale, t);

                        UpdateTransform(position, rotation, localScale);

                        return;
                    }
                    break;
            }
        }

        protected virtual void StartInterpolation(in Vector3 position, in Quaternion rotation, in Vector3 localScale, in int step)
        {
            if (m_parent != null)
            {
                m_handler.start.position = m_parent.transform.InverseTransformPoint(this.transform.position);
                m_handler.start.rotation = (Quaternion.Inverse(m_parent.transform.rotation) * this.transform.rotation).ToVec();
            }
            else
            {
                m_handler.start.position = this.transform.position;
                m_handler.start.rotation = this.transform.rotation.ToVec();
            }
            m_handler.start.localScale = this.transform.localScale;

            m_handler.target.position = position;
            m_handler.target.rotation = rotation.ToVec();
            m_handler.target.localScale = localScale;

            m_handler.step = step;
            m_handler.current = m_handler.step;
        }

        protected virtual void StartInterpolation(in Vector3 position, in Quaternion rotation, in Vector3 localScale) =>
            StartInterpolation(in position, in rotation, in localScale, Math.Max(1, m_interpolationStep * (int)((1 / Time.deltaTime) / INTERPOLATION_BASE_FPS)));

        protected virtual void StopInterpolation()
        {
            ApplyNetworkState(this.transform);
            m_handler.current = -1;
        }

        public void SyncFrom(in int from, in bool immediate, in NetworkRigidbodyTransformState state)
        {
            var localScale = state.transform.localScale;
            var position = state.transform.position;
            var rotation = state.transform.rotation;

            if (immediate)
                StartInterpolation(position, rotation.ToQuaternion(), localScale, 1);
            else
            {
                switch (m_interpolationMode)
                {
                    case InterpolationMode.None:
                        StartInterpolation(position, rotation.ToQuaternion(), localScale, 1);
                        break;
                    case InterpolationMode.Step:
                        StartInterpolation(position, rotation.ToQuaternion(), localScale);
                        break;
                }
            }

            m_rbState.Update(state.rigidbody.used, state.rigidbody.gravity);

            ApplyNetworkState(position, rotation, localScale);

            m_synchronised = true;
        }

        protected virtual void ApplyNetworkStatePosition(in Vector3 position)
        {
            m_networkState.transform.position.x = position.x;
            m_networkState.transform.position.y = position.y;
            m_networkState.transform.position.z = position.z;
        }

        protected virtual void ApplyNetworkStateRotation(in Vector4 rotation)
        {
            m_networkState.transform.rotation.x = rotation.x;
            m_networkState.transform.rotation.y = rotation.y;
            m_networkState.transform.rotation.z = rotation.z;
            m_networkState.transform.rotation.w = rotation.w;
        }

        protected virtual void ApplyNetworkStateRotation(in Quaternion rotation)
        {
            m_networkState.transform.rotation.x = rotation.x;
            m_networkState.transform.rotation.y = rotation.y;
            m_networkState.transform.rotation.z = rotation.z;
            m_networkState.transform.rotation.w = rotation.w;
        }

        protected virtual void ApplyNetworkStateLocalScale(in Vector3 localScale)
        {
            m_networkState.transform.localScale.x = localScale.x;
            m_networkState.transform.localScale.y = localScale.y;
            m_networkState.transform.localScale.z = localScale.z;
        }

        protected virtual void ApplyNetworkState(in Vector3 position, in Quaternion rotation, in Vector3 localScale)
        {
            ApplyNetworkStatePosition(position);
            ApplyNetworkStateRotation(rotation);
            ApplyNetworkStateLocalScale(localScale);
        }

        protected virtual void ApplyNetworkState(in Vector3 position, in Vector4 rotation, in Vector3 localScale) => ApplyNetworkState(position, rotation.ToQuaternion(), localScale);

        protected virtual void ApplyNetworkState(in Transform @transform)
        {
            if (m_parent != null)
            {
                var rotation = (Quaternion.Inverse(m_parent.transform.rotation) * @transform.rotation).ToVec();
                var position = m_parent.transform.InverseTransformPoint(@transform.position);
                ApplyNetworkState(position, rotation, @transform.localScale);
            }
            else
                ApplyNetworkState(@transform.position, @transform.rotation, @transform.localScale);
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

            position = m_networkState.transform.position;
            rotation = m_networkState.transform.rotation;

            Vector3 newPosition; Quaternion newRotation;

            if (m_parent != null)
            {
                newPosition = m_parent.transform.InverseTransformPoint(this.transform.position);
                newRotation = Quaternion.Inverse(m_parent.transform.rotation) * this.transform.rotation;
            }
            else
            {
                newPosition = this.transform.position;
                newRotation = this.transform.rotation;
            }

            if (Vector3.Distance(position, newPosition) > positionThreshold)
            {
                ApplyNetworkStatePosition(newPosition);

                position = newPosition;

                isDirty = true;
            }

            if (Quaternion.Angle(rotation.ToQuaternion(), newRotation) > rotAngleThreshold)
            {
                ApplyNetworkStateRotation(newRotation);

                rotation = newRotation.ToVec();

                isDirty = true;
            }

            localScale = m_networkState.transform.localScale;

            if (Vector3.Distance(m_networkState.transform.localScale, this.transform.localScale) > scaleThreshold)
            {
                ApplyNetworkStateLocalScale(this.transform.localScale);

                localScale = this.transform.localScale;

                isDirty = true;
            }

            return isDirty;
        }

        public virtual bool SkipApplyCurrentTransform() => !Const.Send.HasFlag(m_direction) || !initialized || (m_handler.current > 0);

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
                SendRTC(to, position, rotation, localScale, request, immediate);
        }

        public override void SyncViaWebSocket(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (SkipApplyCurrentTransform())
                return;

            if (ApplyCurrentTransform(out var position, out var rotation, out var localScale) || force)
                SendWS(to, position, rotation, localScale, request, immediate);
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

            ApplyNetworkState(this.transform);

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
