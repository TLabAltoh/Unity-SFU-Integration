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

            public Vector3 ring;
            public Vector3 pinky;
            public Vector3 thumb;
            public Vector3 index;
            public Vector3 middle;
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

        public static readonly float INTERPOLATION_BASE_FPS = 30;

        protected struct InterpolationHandler
        {
            public int newFingers;
            public int step;

            public SerializableHandTracking start;
            public SerializableHandTracking target;

            public InterpolationHandler(int dummy = 0)
            {
                this.newFingers = -1;
                this.step = -1;

                this.start = new SerializableHandTracking();
                this.target = new SerializableHandTracking();
            }
        }

        protected InterpolationHandler m_handler = new InterpolationHandler(0);

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
            a.ring = this.transform.InverseTransformPoint(m_ring.position);
            a.pinky = this.transform.InverseTransformPoint(m_pinky.position);
            a.thumb = this.transform.InverseTransformPoint(m_thumb.position);
            a.index = this.transform.InverseTransformPoint(m_index.position);
            a.middle = this.transform.InverseTransformPoint(m_middle.position);
            return a;
        }

        protected void UpdateFingers(in SerializableHandTracking fingers)
        {
            m_ring.position = this.transform.TransformPoint(fingers.ring);
            m_pinky.position = this.transform.TransformPoint(fingers.pinky);
            m_thumb.position = this.transform.TransformPoint(fingers.thumb);
            m_index.position = this.transform.TransformPoint(fingers.index);
            m_middle.position = this.transform.TransformPoint(fingers.middle);
        }

        protected virtual void GetInterpolatedFingers(in SerializableHandTracking start, in SerializableHandTracking target, out SerializableHandTracking interpolated, float t)
        {
            interpolated = new SerializableHandTracking();
            interpolated.ring = Vector3.Lerp(start.ring, target.ring, t);
            interpolated.pinky = Vector3.Lerp(start.pinky, target.pinky, t);
            interpolated.thumb = Vector3.Lerp(start.thumb, target.thumb, t);
            interpolated.index = Vector3.Lerp(start.index, target.index, t);
            interpolated.middle = Vector3.Lerp(start.middle, target.middle, t);
        }

        protected virtual void InterpolateTransform()
        {
            if (m_handler.newFingers > 0)
                m_handler.newFingers--;

            if (m_handler.newFingers == 0)
            {
                UpdateFingers(m_handler.target);
                StopInterpolation();
                return;
            }

            switch (m_interpolationMode)
            {
                case InterpolationMode.None:
                    break;
                case InterpolationMode.Step:
                    if (m_handler.newFingers > 0)
                    {
                        var t = (float)(m_handler.step - m_handler.newFingers) / m_handler.step;

                        GetInterpolatedFingers(m_handler.start, m_handler.target, out var interpolated, t);

                        UpdateFingers(interpolated);

                        return;
                    }
                    break;
            }
        }

        protected virtual void Inverse(ref SerializableHandTracking dst)
        {
            dst.ring = this.transform.InverseTransformPoint(m_ring.position);
            dst.pinky = this.transform.InverseTransformPoint(m_pinky.position);
            dst.thumb = this.transform.InverseTransformPoint(m_thumb.position);
            dst.index = this.transform.InverseTransformPoint(m_index.position);
            dst.middle = this.transform.InverseTransformPoint(m_middle.position);
        }

        protected virtual void Inverse(in SerializableHandTracking src, ref SerializableHandTracking dst)
        {
            dst.ring = this.transform.InverseTransformPoint(src.ring);
            dst.pinky = this.transform.InverseTransformPoint(src.pinky);
            dst.thumb = this.transform.InverseTransformPoint(src.thumb);
            dst.index = this.transform.InverseTransformPoint(src.index);
            dst.middle = this.transform.InverseTransformPoint(src.middle);
        }

        protected virtual void StartInterpolation(in SerializableHandTracking fingers)
        {
            Inverse(ref m_handler.start);
            m_handler.target = fingers;

            m_handler.step = Math.Max(1, m_interpolationStep * (int)((1 / Time.deltaTime) / INTERPOLATION_BASE_FPS));
            m_handler.newFingers = m_handler.step;
        }

        protected virtual void StopInterpolation()
        {
            ApplyFingers(GetCurrent());
            m_handler.newFingers = -1;
        }

        public void SyncFrom(in int from, in bool immediate, in NetworkOVRHandTrackingState state)
        {
            if (immediate)
                UpdateFingers(state.fingers);
            else
            {
                switch (m_interpolationMode)
                {
                    case InterpolationMode.None:
                        UpdateFingers(state.fingers);
                        break;
                    case InterpolationMode.Step:
                        StartInterpolation(state.fingers);
                        break;
                }
            }

            ApplyFingers(state.fingers);

            m_synchronised = true;
        }

        protected virtual void ApplyFingers(in SerializableHandTracking fingers) => m_networkState.fingers = fingers;

        public virtual bool ApplyCurrentFingers(out SerializableHandTracking fingers)
        {
            bool isDirty = false;

            m_networkState.id = m_networkId.id;

            var newFingers = GetCurrent();

            fingers = m_networkState.fingers;

            var sum = 0.0f;
            sum += Vector3.Distance(fingers.ring, newFingers.ring);
            sum += Vector3.Distance(fingers.pinky, newFingers.pinky);
            sum += Vector3.Distance(fingers.thumb, newFingers.thumb);
            sum += Vector3.Distance(fingers.index, newFingers.index);
            sum += Vector3.Distance(fingers.middle, newFingers.middle);

            if (sum > positionThreshold * 5)
            {
                ApplyFingers(newFingers);

                fingers = newFingers;

                isDirty = true;
            }

            return isDirty;
        }

        public virtual bool SkipApplyCurrentTransform() => !Const.Send.HasFlag(m_direction) || !initialized || (m_handler.newFingers > 0);

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

            if (ApplyCurrentFingers(out var fingers) || force)
                SendRTC(to, fingers, request, immediate);
        }

        public override void SyncViaWebSocket(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (SkipApplyCurrentTransform())
                return;

            if (ApplyCurrentFingers(out var fingers) || force)
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

            ApplyFingers(GetCurrent());
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
