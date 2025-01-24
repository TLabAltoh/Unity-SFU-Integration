using System;
using UnityEngine;
using Unity.Mathematics;
using TLab.SFU;
using TLab.SFU.Network;

using static TLab.SFU.UnsafeUtility;

namespace TLab.VRProjct.Avator
{
    using Registry = Registry<Address64, NetworkOVRHandTracking>;

    public class NetworkOVRHandTracking : NetworkObject
    {
        #region STRUCT

        [Serializable]
        public struct NetworkOVRHandTrackingState
        {
            public Address64 id;

            public SerializableHandTracking fingers;
        }

        [Serializable]
        public struct SerializableHandTracking
        {
            public Address64 id;

            public Vector3 thumb;
            public Vector3 index;
            public Vector3 middle;
            public Vector3 ring;
            public Vector3 pinky;
        }

        #endregion STRUCT

        #region MESSAGE

        [Serializable, Message(typeof(MSG_NetworkOVRHandTracking))]
        public class MSG_NetworkOVRHandTracking : MSG_Sync
        {
            #region CONSTANT

            private const int SEND_HEADER_LEN = 8, RECV_HEADER_LEN = 13;    // SEND: to (4) + msgId (4), RECV: typ (1) + from (4) + to (4) + msgId (4)

            private const int NETWORK_ID_FIELD_LEN = 8;

            private const int BOOL_FIELD_OFFSET = NETWORK_ID_FIELD_LEN, BOOL_FIELD_LEN = 2;  // request (1) + immediate (1)

            private const int HAND_FIELD_OFFSET = BOOL_FIELD_OFFSET + BOOL_FIELD_LEN, FINGERS_FIELD_LEN = 15;    // ((5 * 3) * 4)

#if TLAB_VRPROJ_USE_HALF_FLOAT
            private const int FINGERS_FIELD_VALUE_SIZE = 2;   // 16 bit
#else
            private const int FINGERS_FIELD_VALUE_SIZE = sizeof(float);
#endif

            private const int PAYLOAD_LEN = NETWORK_ID_FIELD_LEN + BOOL_FIELD_LEN + FINGERS_FIELD_LEN * FINGERS_FIELD_VALUE_SIZE;

            #endregion CONSTANT

            public NetworkOVRHandTrackingState state;

            private static byte[] m_packet = new byte[SEND_HEADER_LEN + PAYLOAD_LEN];

#if TLAB_VRPROJ_USE_HALF_FLOAT
            private static half[] m_fingers = new half[FINGERS_FIELD_LEN];
#else
            private static float[] m_fingers = new float[FINGERS_FIELD_LEN];
#endif

            public MSG_NetworkOVRHandTracking(NetworkOVRHandTrackingState state) : base()
            {
                this.state = state;
            }

            public MSG_NetworkOVRHandTracking(byte[] bytes) : base(bytes) { }

            public override byte[] Marshall()
            {
#if TLAB_VRPROJ_USE_HALF_FLOAT
                m_fingers[0] = math.half(state.fingers.thumb.x);
                m_fingers[1] = math.half(state.fingers.thumb.y);
                m_fingers[2] = math.half(state.fingers.thumb.z);

                m_fingers[3] = math.half(state.fingers.index.x);
                m_fingers[4] = math.half(state.fingers.index.y);
                m_fingers[5] = math.half(state.fingers.index.z);

                m_fingers[6] = math.half(state.fingers.middle.x);
                m_fingers[7] = math.half(state.fingers.middle.y);
                m_fingers[8] = math.half(state.fingers.middle.z);

                m_fingers[9] = math.half(state.fingers.ring.x);
                m_fingers[10] = math.half(state.fingers.ring.y);
                m_fingers[11] = math.half(state.fingers.ring.z);

                m_fingers[12] = math.half(state.fingers.pinky.x);
                m_fingers[13] = math.half(state.fingers.pinky.y);
                m_fingers[14] = math.half(state.fingers.pinky.z);
#else
                m_fingers[0] = state.fingers.thumb.x;
                m_fingers[1] = state.fingers.thumb.y;
                m_fingers[2] = state.fingers.thumb.z;

                m_fingers[3] = state.fingers.index.x;
                m_fingers[4] = state.fingers.index.y;
                m_fingers[5] = state.fingers.index.z;

                m_fingers[6] = state.fingers.middle.x;
                m_fingers[7] = state.fingers.middle.y;
                m_fingers[8] = state.fingers.middle.z;

                m_fingers[9] = state.fingers.ring.x;
                m_fingers[10] = state.fingers.ring.y;
                m_fingers[11] = state.fingers.ring.z;

                m_fingers[12] = state.fingers.pinky.x;
                m_fingers[13] = state.fingers.pinky.y;
                m_fingers[14] = state.fingers.pinky.z;
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

                        Copy(m_fingers, p + HAND_FIELD_OFFSET, FINGERS_FIELD_LEN);
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

                        Copy(p + HAND_FIELD_OFFSET, m_fingers, FINGERS_FIELD_LEN * FINGERS_FIELD_VALUE_SIZE);

                        state.fingers.thumb.x = m_fingers[0];
                        state.fingers.thumb.y = m_fingers[1];
                        state.fingers.thumb.z = m_fingers[2];

                        state.fingers.index.x = m_fingers[3];
                        state.fingers.index.y = m_fingers[4];
                        state.fingers.index.z = m_fingers[5];

                        state.fingers.middle.x = m_fingers[6];
                        state.fingers.middle.y = m_fingers[7];
                        state.fingers.middle.z = m_fingers[8];

                        state.fingers.ring.x = m_fingers[9];
                        state.fingers.ring.y = m_fingers[10];
                        state.fingers.ring.z = m_fingers[11];

                        state.fingers.pinky.x = m_fingers[12];
                        state.fingers.pinky.y = m_fingers[13];
                        state.fingers.pinky.z = m_fingers[14];
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

        [Header("OVR Hand")]
        [SerializeField] private OVRHand.Hand m_fingers;

        [Header("Finger")]
        [SerializeField] private Transform m_thumb;
        [SerializeField] private Transform m_index;
        [SerializeField] private Transform m_middle;
        [SerializeField] private Transform m_ring;
        [SerializeField] private Transform m_pinky;

        [Header("Threshold")]

        public float positionThreshold = 0.001f;

        [Header("Interpolation")]

        [SerializeField] protected InterpolationMode m_interpolationMode = InterpolationMode.None;
        [SerializeField, Min(1)] protected int m_interpolationStep = 3;

        protected SerializableHandTracking m_prev;

        protected NetworkOVRHandTrackingState m_networkState;

        protected SerializableHandTracking m_delta;

        public static readonly float INTERPOLATION_BASE_FPS = 30;

        protected struct InterpolationState
        {
            public int current;
            public int step;

            public SerializableHandTracking start;
            public SerializableHandTracking target;

            public InterpolationState(int dummy = 0)
            {
                this.current = -1;
                this.step = -1;

                this.start = new SerializableHandTracking();
                this.target = new SerializableHandTracking();
            }
        }

        protected InterpolationState m_interpolationState = new InterpolationState(0);

        public InterpolationMode interpolationMode => m_interpolationMode;

        public int interpolationStep => m_interpolationStep;

        private static MSG_NetworkOVRHandTracking m_packet = new MSG_NetworkOVRHandTracking(new NetworkOVRHandTrackingState());

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public override void OnSyncRequest(int from)
        {
            base.OnSyncRequest(from);

            SyncViaWebSocket(from, true, true, true);
        }

        public SerializableHandTracking GetCurrent()
        {
            var a = new SerializableHandTracking();
            a.thumb = m_thumb.position;
            a.index = m_index.position;
            a.middle = m_middle.position;
            a.ring = m_ring.position;
            a.pinky = m_pinky.position;
            return a;
        }

        protected void UpdateTransform(in SerializableHandTracking fingers)
        {
            m_thumb.position = fingers.thumb;
            m_index.position = fingers.index;
            m_middle.position = fingers.middle;
            m_ring.position = fingers.ring;
            m_pinky.position = fingers.pinky;
        }

        protected virtual void GetInterpolatedTransform(in SerializableHandTracking start, in SerializableHandTracking target, out SerializableHandTracking interpolated, float t)
        {
            interpolated = new SerializableHandTracking();
            interpolated.thumb = Vector3.Lerp(start.thumb, target.thumb, t);
            interpolated.index = Vector3.Lerp(start.index, target.index, t);
            interpolated.middle = Vector3.Lerp(start.middle, target.middle, t);
            interpolated.ring = Vector3.Lerp(start.ring, target.ring, t);
            interpolated.pinky = Vector3.Lerp(start.pinky, target.pinky, t);
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

                        GetInterpolatedTransform(m_interpolationState.start, m_interpolationState.target, out var interpolated, t);

                        UpdateTransform(interpolated);

                        return;
                    }
                    break;
            }
        }

        protected virtual void StartInterpolation(in SerializableHandTracking fingers)
        {
            m_interpolationState.start.thumb = m_thumb.position;
            m_interpolationState.start.index = m_index.position;
            m_interpolationState.start.middle = m_middle.position;
            m_interpolationState.start.ring = m_ring.position;
            m_interpolationState.start.pinky = m_pinky.position;

            m_interpolationState.target.thumb = fingers.thumb;
            m_interpolationState.target.index = fingers.index;
            m_interpolationState.target.middle = fingers.middle;
            m_interpolationState.target.ring = fingers.ring;
            m_interpolationState.target.pinky = fingers.pinky;

            m_interpolationState.step = Math.Max(1, m_interpolationStep * (int)((1 / Time.deltaTime) / INTERPOLATION_BASE_FPS));
            m_interpolationState.current = m_interpolationState.step;
        }

        protected virtual void StopInterpolation()
        {
            ApplyHandPositionAndDelta(GetCurrent());
            m_interpolationState.current = -1;
        }

        public void SyncFrom(in int from, in bool immediate, in NetworkOVRHandTrackingState state)
        {
            var fingers = new SerializableHandTracking();
            fingers.thumb = this.transform.TransformPoint(state.fingers.thumb);
            fingers.index = this.transform.TransformPoint(state.fingers.index);
            fingers.middle = this.transform.TransformPoint(state.fingers.middle);
            fingers.ring = this.transform.TransformPoint(state.fingers.ring);
            fingers.pinky = this.transform.TransformPoint(state.fingers.pinky);

            if (immediate)
                UpdateTransform(fingers);
            else
            {
                switch (m_interpolationMode)
                {
                    case InterpolationMode.None:
                        UpdateTransform(fingers);
                        break;
                    case InterpolationMode.Step:
                        StartInterpolation(fingers);
                        break;
                }
            }

            ApplyHandPositionAndDelta(fingers);

            m_synchronised = true;
        }

        protected virtual void ApplyHandPositionAndDelta(in SerializableHandTracking fingers)
        {
            var delta = new SerializableHandTracking();
            delta.thumb = this.transform.InverseTransformPoint(fingers.thumb);
            delta.index = this.transform.InverseTransformPoint(fingers.index);
            delta.middle = this.transform.InverseTransformPoint(fingers.middle);
            delta.ring = this.transform.InverseTransformPoint(fingers.ring);
            delta.pinky = this.transform.InverseTransformPoint(fingers.pinky);

            ApplyHandPosition(fingers);
            ApplyHandPositionDelta(delta);
        }

        protected virtual void ApplyHandPosition(in SerializableHandTracking fingers) => m_networkState.fingers = fingers;

        protected virtual void ApplyHandPositionDelta(in SerializableHandTracking delta) => m_delta = delta;

        public virtual bool ApplyCurrentPositionAndDelta(out SerializableHandTracking fingers)
        {
            bool isDirty = false;

            m_networkState.id = m_networkId.id;

            var current = GetCurrent();

            var delta = new SerializableHandTracking();
            delta.thumb = this.transform.InverseTransformPoint(current.thumb);
            delta.index = this.transform.InverseTransformPoint(current.index);
            delta.middle = this.transform.InverseTransformPoint(current.middle);
            delta.ring = this.transform.InverseTransformPoint(current.ring);
            delta.pinky = this.transform.InverseTransformPoint(current.pinky);

            fingers = m_delta;

            var sum = Vector3.Distance(m_delta.thumb, delta.thumb);
            sum += Vector3.Distance(m_delta.index, delta.index);
            sum += Vector3.Distance(m_delta.middle, delta.middle);
            sum += Vector3.Distance(m_delta.ring, delta.ring);
            sum += Vector3.Distance(m_delta.pinky, delta.pinky);

            if (sum > positionThreshold * 5)
            {
                ApplyHandPosition(current);
                ApplyHandPositionDelta(delta);

                fingers = delta;

                isDirty = true;
            }

            return isDirty;
        }

        public virtual bool SkipApplyCurrentTransform() => !Const.Send.HasFlag(m_direction) || !initialized || (m_interpolationState.current > 0);

        private void SetSendPacket(in SerializableHandTracking fingers, bool request = false, bool immediate = false)
        {
            m_packet.state.id = m_networkState.id;

            m_packet.state.fingers = fingers;

            m_packet.request = request;
            m_packet.immediate = immediate;
        }

        private void SendRTC(int to, in SerializableHandTracking fingers, bool request = false, bool immediate = false)
        {
            SetSendPacket(fingers, request, immediate);
            NetworkClient.SendRTC(to, m_packet.Marshall());

            m_synchronised = false;
        }

        private void SendWS(int to, in SerializableHandTracking fingers, bool request = false, bool immediate = false)
        {
            SetSendPacket(fingers, request, immediate);
            NetworkClient.SendWS(to, m_packet.Marshall());

            m_synchronised = false;
        }

        public override void SyncViaWebRTC(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (SkipApplyCurrentTransform())
                return;

            if (ApplyCurrentPositionAndDelta(out var fingers) || force)
                SendRTC(to, fingers, request, immediate);
        }

        public override void SyncViaWebSocket(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (SkipApplyCurrentTransform())
                return;

            if (ApplyCurrentPositionAndDelta(out var fingers) || force)
                SendWS(to, fingers, request, immediate);
        }

        protected override void RegisterOnMessage()
        {
            base.RegisterOnMessage();

            NetworkClient.RegisterOnMessage<MSG_NetworkOVRHandTracking>((from, to, bytes) =>
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

        protected override void Start()
        {
            base.Start();

            ApplyHandPositionAndDelta(GetCurrent());
        }

        protected override void Update()
        {
            if (Const.Recv.HasFlag(m_direction))
                InterpolateTransform();

            if (Const.Send.HasFlag(m_direction))
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
