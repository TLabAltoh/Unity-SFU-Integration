using System;
using UnityEngine;
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

            public SerializableHandTracking hand;
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

            private const int HAND_FIELD_OFFSET = BOOL_FIELD_OFFSET + BOOL_FIELD_LEN, HAND_FIELD_LEN = 15;    // ((5 * 3) * 4)

            private const int PAYLOAD_LEN = NETWORK_ID_FIELD_LEN + BOOL_FIELD_LEN + HAND_FIELD_LEN * sizeof(float);

            #endregion CONSTANT

            public NetworkOVRHandTrackingState state;

            private static byte[] m_packetBuf = new byte[SEND_HEADER_LEN + PAYLOAD_LEN];

            private static float[] m_handBuf = new float[HAND_FIELD_LEN];

            public MSG_NetworkOVRHandTracking(NetworkOVRHandTrackingState state) : base()
            {
                this.state = state;
            }

            public MSG_NetworkOVRHandTracking(byte[] bytes) : base(bytes) { }

            public override byte[] Marshall()
            {
                m_handBuf[0] = state.hand.thumb.x;
                m_handBuf[1] = state.hand.thumb.y;
                m_handBuf[2] = state.hand.thumb.z;

                m_handBuf[3] = state.hand.index.x;
                m_handBuf[4] = state.hand.index.y;
                m_handBuf[5] = state.hand.index.z;

                m_handBuf[6] = state.hand.middle.x;
                m_handBuf[7] = state.hand.middle.y;
                m_handBuf[8] = state.hand.middle.z;

                m_handBuf[9] = state.hand.ring.x;
                m_handBuf[10] = state.hand.ring.y;
                m_handBuf[11] = state.hand.ring.z;

                m_handBuf[12] = state.hand.pinky.x;
                m_handBuf[13] = state.hand.pinky.y;
                m_handBuf[14] = state.hand.pinky.z;

                unsafe
                {
                    fixed (byte* m_packetBufPtr = m_packetBuf)
                    {
                        Copy(msgId, m_packetBufPtr + sizeof(int));

                        var payloadPtr = m_packetBufPtr + SEND_HEADER_LEN;

                        state.id.CopyTo(payloadPtr);

                        Copy(request, payloadPtr + BOOL_FIELD_OFFSET + 0);
                        Copy(immediate, payloadPtr + BOOL_FIELD_OFFSET + 1);

                        Copy(m_handBuf, payloadPtr + HAND_FIELD_OFFSET, HAND_FIELD_LEN);
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

                        Copy(payloadPtr + HAND_FIELD_OFFSET, m_handBuf, HAND_FIELD_LEN * sizeof(float));

                        state.hand.thumb.x = m_handBuf[0];
                        state.hand.thumb.y = m_handBuf[1];
                        state.hand.thumb.z = m_handBuf[2];

                        state.hand.index.x = m_handBuf[3];
                        state.hand.index.y = m_handBuf[4];
                        state.hand.index.z = m_handBuf[5];

                        state.hand.middle.x = m_handBuf[6];
                        state.hand.middle.y = m_handBuf[7];
                        state.hand.middle.z = m_handBuf[8];

                        state.hand.ring.x = m_handBuf[9];
                        state.hand.ring.y = m_handBuf[10];
                        state.hand.ring.z = m_handBuf[11];

                        state.hand.pinky.x = m_handBuf[12];
                        state.hand.pinky.y = m_handBuf[13];
                        state.hand.pinky.z = m_handBuf[14];
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
        [SerializeField] private OVRHand.Hand m_hand;

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

        protected void UpdateTransform(in SerializableHandTracking hand)
        {
            m_thumb.position = hand.thumb;
            m_index.position = hand.index;
            m_middle.position = hand.middle;
            m_ring.position = hand.ring;
            m_pinky.position = hand.pinky;
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

        protected virtual void StartInterpolation(in SerializableHandTracking hand)
        {
            m_interpolationState.start.thumb = m_thumb.position;
            m_interpolationState.start.index = m_index.position;
            m_interpolationState.start.middle = m_middle.position;
            m_interpolationState.start.ring = m_ring.position;
            m_interpolationState.start.pinky = m_pinky.position;

            m_interpolationState.target.thumb = hand.thumb;
            m_interpolationState.target.index = hand.index;
            m_interpolationState.target.middle = hand.middle;
            m_interpolationState.target.ring = hand.ring;
            m_interpolationState.target.pinky = hand.pinky;

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
            var hand = new SerializableHandTracking();
            hand.thumb = this.transform.TransformPoint(state.hand.thumb);
            hand.index = this.transform.TransformPoint(state.hand.index);
            hand.middle = this.transform.TransformPoint(state.hand.middle);
            hand.ring = this.transform.TransformPoint(state.hand.ring);
            hand.pinky = this.transform.TransformPoint(state.hand.pinky);

            if (immediate)
                UpdateTransform(hand);
            else
            {
                switch (m_interpolationMode)
                {
                    case InterpolationMode.None:
                        UpdateTransform(hand);
                        break;
                    case InterpolationMode.Step:
                        StartInterpolation(hand);
                        break;
                }
            }

            ApplyHandPositionAndDelta(hand);

            m_synchronised = true;
        }

        protected virtual void ApplyHandPositionAndDelta(in SerializableHandTracking hand)
        {
            var delta = new SerializableHandTracking();
            delta.thumb = this.transform.InverseTransformPoint(hand.thumb);
            delta.index = this.transform.InverseTransformPoint(hand.index);
            delta.middle = this.transform.InverseTransformPoint(hand.middle);
            delta.ring = this.transform.InverseTransformPoint(hand.ring);
            delta.pinky = this.transform.InverseTransformPoint(hand.pinky);

            ApplyHandPosition(hand);
            ApplyHandPositionDelta(delta);
        }

        protected virtual void ApplyHandPosition(in SerializableHandTracking hand) => m_networkState.hand = hand;

        protected virtual void ApplyHandPositionDelta(in SerializableHandTracking delta) => m_delta = delta;

        public virtual bool ApplyCurrentPositionAndDelta(out SerializableHandTracking hand)
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

            hand = m_delta;

            var sum = Vector3.Distance(m_delta.thumb, delta.thumb);
            sum += Vector3.Distance(m_delta.index, delta.index);
            sum += Vector3.Distance(m_delta.middle, delta.middle);
            sum += Vector3.Distance(m_delta.ring, delta.ring);
            sum += Vector3.Distance(m_delta.pinky, delta.pinky);

            if (sum > positionThreshold * 5)
            {
                ApplyHandPosition(current);
                ApplyHandPositionDelta(delta);

                hand = delta;

                isDirty = true;
            }

            return isDirty;
        }

        public virtual bool SkipApplyCurrentTransform() => !Const.Send.HasFlag(m_direction) || !initialized || (m_interpolationState.current > 0);

        private void SetSendPacket(in SerializableHandTracking hand, bool request = false, bool immediate = false)
        {
            m_packet.state.id = m_networkState.id;

            m_packet.state.hand = hand;

            m_packet.request = request;
            m_packet.immediate = immediate;
        }

        private void SendRTC(int to, in SerializableHandTracking hand, bool request = false, bool immediate = false)
        {
            SetSendPacket(hand, request, immediate);
            NetworkClient.SendRTC(to, m_packet.Marshall());

            m_synchronised = false;
        }

        private void SendWS(int to, in SerializableHandTracking hand, bool request = false, bool immediate = false)
        {
            SetSendPacket(hand, request, immediate);
            NetworkClient.SendWS(to, m_packet.Marshall());

            m_synchronised = false;
        }

        public override void SyncViaWebRTC(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (SkipApplyCurrentTransform())
                return;

            if (ApplyCurrentPositionAndDelta(out var hand) || force)
                SendRTC(to, hand, request, immediate);
        }

        public override void SyncViaWebSocket(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (SkipApplyCurrentTransform())
                return;

            if (ApplyCurrentPositionAndDelta(out var hand) || force)
                SendWS(to, hand, request, immediate);
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
