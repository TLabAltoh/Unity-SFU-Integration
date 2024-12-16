using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;
using TLab.SFU.Network.Json;

namespace TLab.SFU.Network
{
    public class WebRTCClient : SfuClient
    {
        private RTCPeerConnection m_pc;
        private RTCDataChannel m_dc;

        private MediaStream m_sMediaStream;
        private MediaStream m_rMediaStream;
        private UnityEvent<MediaStreamTrackEvent> m_onAddTrack;

        private ClientType m_clientType;
        private IceExchangeOption m_iceExchangeOption;

        private Coroutine m_signalingTask;
        private WebSocket m_signalingSocket;
        private Queue<RTCIceCandidate> m_candidates = new Queue<RTCIceCandidate>();

        public RTCSessionDescription localDescription
        {
            get
            {
                if (m_pc != null)
                {
                    return m_pc.LocalDescription;
                }

                return new RTCSessionDescription();
            }
        }

        public RTCSessionDescription remoteDescription
        {
            get
            {
                if (m_pc != null)
                {
                    return m_pc.RemoteDescription;
                }

                return new RTCSessionDescription();
            }
        }

        public bool connected
        {
            get
            {
                if (m_pc == null)
                    return false;
                return (m_pc.ConnectionState == RTCPeerConnectionState.Connected);
            }
        }

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        #region STRUCT

        public enum ClientType
        {
            Whip,
            Whep,
        };

        [Serializable]
        public class Signaling : IRequest, IResponse<Signaling>
        {
            public bool isCandidate;
            public string sdp;
            public string session;
            public string candidate;

            public Signaling(string json) => FromJsonOverwrite(json);

            public Signaling(bool isCandidate, string sdp, string session, string candidate)
            {
                this.isCandidate = isCandidate;
                this.sdp = sdp;
                this.session = session;
                this.candidate = candidate;
            }

            [Serializable]
            public class RustFormat
            {
                public bool is_candidate;
                public string sdp;
                public string session;
                public string candidate;

                public RustFormat() { }

                public RustFormat(bool isCandidate, string sdp, string session, string candidate)
                {
                    this.is_candidate = isCandidate;
                    this.sdp = sdp;
                    this.session = session;
                    this.candidate = candidate;
                }
            }

            public string ToJson() => JsonUtility.ToJson(new RustFormat(isCandidate, sdp, session, candidate));

            public void FromJsonOverwrite(string json)
            {
                var tmp = JsonUtility.FromJson<RustFormat>(json);
                isCandidate = tmp.is_candidate;
                sdp = tmp.sdp;
                session = tmp.session;
                candidate = tmp.candidate;
            }
        }

        [Serializable]
        public class StreamRequest : RequestAuth
        {
            public string stream;
            public string offer;

            public StreamRequest(RequestAuth auth, string stream, string offer) : base(auth)
            {
                this.stream = stream;
                this.offer = offer;
            }

            [Serializable]
            public new class RustFormat
            {
                public int room_id;
                public int user_id;
                public uint token;
                public string stream;
                public string offer;
                public string shared_key;

                public RustFormat(int roomId, string sharedKey, int userId, uint token, string stream, string offer)
                {
                    this.room_id = roomId;
                    this.user_id = userId;
                    this.token = token;
                    this.stream = stream;
                    this.offer = offer;
                    this.shared_key = sharedKey;
                }
            }

            public override string ToJson() => JsonUtility.ToJson(new RustFormat(roomId, sharedKey, userId, token, stream, offer));
        }

        #endregion STRUCT

        public enum IceExchangeOption
        {
            Triccle,
            Vanilla,
        };

        public static UnityEvent<MediaStreamTrackEvent> CreateOnAddTrack(params UnityAction<MediaStreamTrackEvent>[] @actions)
        {
            var @event = new UnityEvent<MediaStreamTrackEvent>();
            foreach (var @action in @actions)
                @event.AddListener(@action);
            return @event;
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, (UnityEvent, UnityEvent<int>) onOpen, (UnityEvent, UnityEvent<int>) onClose, UnityEvent onError, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, UnityEvent<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle)
        {
            var client = new WebRTCClient(mono, adapter, stream, onMessage, onOpen, onClose, onError, onAddTrack, iceExchangeOption);

            client.CreatePeerConnection(ClientType.Whip, dataChannelCnf);

            if (audioSource)
                client.InitAudioSender(audioSource);

            if (videoSource)
                client.InitVideoSender(videoSource);

            mono.StartCoroutine(client.CreateOffer());

            return client;
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onMessage, (UnityAction, UnityAction<int>) onOpen, (UnityAction, UnityAction<int>) onClose, UnityAction onError, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, UnityAction<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle)
        {
            return Whip(mono, adapter, stream, CreateEvent(onMessage), (CreateEvent(onOpen.Item1), CreateEvent(onOpen.Item2)), (CreateEvent(onClose.Item1), CreateEvent(onClose.Item2)), CreateEvent(onError), dataChannelCnf, videoSource, audioSource, CreateOnAddTrack(onAddTrack), iceExchangeOption);
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle)
        {
            return Whip(mono, adapter, stream, CreateEvent<int, int, byte[]>(null), (CreateEvent(null), CreateEvent<int>(null)), (CreateEvent(null), CreateEvent<int>(null)), CreateEvent(null), dataChannelCnf, videoSource, audioSource, CreateOnAddTrack(null), iceExchangeOption);
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, (UnityEvent, UnityEvent<int>) onOpen, (UnityEvent, UnityEvent<int>) onClose, UnityEvent onError, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, UnityEvent<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle)
        {
            var client = new WebRTCClient(mono, adapter, stream, onMessage, onOpen, onClose, onError, onAddTrack, iceExchangeOption);

            client.CreatePeerConnection(ClientType.Whep, dataChannelCnf);

            if (receiveAudio)
                client.InitReceiver(TrackKind.Audio);

            if (receiveVideo)
                client.InitReceiver(TrackKind.Video);

            mono.StartCoroutine(client.CreateOffer());

            return client;
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onMessage, (UnityAction, UnityAction<int>) onOpen, (UnityAction, UnityAction<int>) onClose, UnityAction onError, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, UnityAction<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle)
        {
            return Whep(mono, adapter, stream, CreateEvent(onMessage), (CreateEvent(onOpen.Item1), CreateEvent(onOpen.Item2)), (CreateEvent(onClose.Item1), CreateEvent(onClose.Item2)), CreateEvent(onError), dataChannelCnf, receiveVideo, receiveAudio, CreateOnAddTrack(onAddTrack), iceExchangeOption);
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle)
        {
            return Whep(mono, adapter, stream, CreateEvent<int, int, byte[]>(null), (CreateEvent(null), CreateEvent<int>(null)), (CreateEvent(null), CreateEvent<int>(null)), CreateEvent(null), dataChannelCnf, receiveVideo, receiveAudio, CreateOnAddTrack(null), iceExchangeOption);
        }

        public WebRTCClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, (UnityEvent, UnityEvent<int>) onOpen, (UnityEvent, UnityEvent<int>) onClose, UnityEvent onError, UnityEvent<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle) : base(mono, adapter, stream, onMessage, onOpen, onClose, onError)
        {
            m_iceExchangeOption = iceExchangeOption;
            m_onAddTrack = onAddTrack;
        }

        public WebRTCClient(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onMessage, (UnityAction, UnityAction<int>) onOpen, (UnityAction, UnityAction<int>) onClose, UnityAction onError, UnityAction<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle) :
            this(mono, adapter, stream, CreateEvent(onMessage), (CreateEvent(onOpen.Item1), CreateEvent(onOpen.Item2)), (CreateEvent(onClose.Item1), CreateEvent(onClose.Item2)), CreateEvent(onError), CreateOnAddTrack(onAddTrack), iceExchangeOption)
        {

        }

        public WebRTCClient(MonoBehaviour mono, Adapter adapter, string stream, IceExchangeOption iceExchangeOption = IceExchangeOption.Triccle) :
            this(mono, adapter, stream, CreateEvent<int, int, byte[]>(null), (CreateEvent(null), CreateEvent<int>(null)), (CreateEvent(null), CreateEvent<int>(null)), CreateEvent(null), CreateOnAddTrack(null), iceExchangeOption)
        {

        }

        public void SetCallback(UnityAction<int, int, byte[]> callback) => m_onMessage.AddListener(callback);

        private void OnIceCandidate(RTCIceCandidate candidate) => m_candidates.Enqueue(candidate);

        private void CancelSignalingTask()
        {
            m_signalingSocket?.Close();
            m_signalingSocket = null;

            if (m_signalingTask != null)
            {
                m_mono.StopCoroutine(m_signalingTask);
                m_signalingTask = null;
            }
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
            switch (state)
            {
                case RTCPeerConnectionState.New:
                    Debug.Log(THIS_NAME + $"{m_stream} : RTCPeerConnectionState: New");
                    break;
                case RTCPeerConnectionState.Connecting:
                    Debug.Log(THIS_NAME + $"{m_stream} : RTCPeerConnectionState: Connecting");
                    break;
                case RTCPeerConnectionState.Connected:
                    Debug.Log(THIS_NAME + $"{m_stream} : RTCPeerConnectionState: Connected");
                    OnOpen1();
                    break;
                case RTCPeerConnectionState.Disconnected:
                    Debug.Log(THIS_NAME + $"{m_stream} : RTCPeerConnectionState: Disconnected");
                    break;
                case RTCPeerConnectionState.Failed:
                    Debug.Log(THIS_NAME + $"{m_stream} : RTCPeerConnectionState: Failed");
                    OnError();
                    break;
                case RTCPeerConnectionState.Closed:
                    Debug.Log(THIS_NAME + $"{m_stream} : RTCPeerConnectionState: Closed");
                    OnClose1();
                    break;
                default:
                    break;
            }
        }

        private void OnIceGatheringStateChange(RTCIceGatheringState state)
        {
            switch (state)
            {
                case RTCIceGatheringState.New:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceGatheringState: New");
                    break;
                case RTCIceGatheringState.Gathering:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceGatheringState: Gathering");
                    break;
                case RTCIceGatheringState.Complete:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceGatheringState: Complete");
                    if (m_iceExchangeOption == IceExchangeOption.Vanilla)
                    {
                        CancelSignalingTask();
                        m_signalingTask = m_mono.StartCoroutine(ExchangeSDP());
                    }
                    break;
            }
        }

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            switch (state)
            {
                case RTCIceConnectionState.New:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceConnectionState: New");
                    break;
                case RTCIceConnectionState.Checking:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceConnectionState: Checking");
                    break;
                case RTCIceConnectionState.Closed:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceConnectionState: Closed");
                    break;
                case RTCIceConnectionState.Completed:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceConnectionState: Completed");
                    break;
                case RTCIceConnectionState.Connected:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceConnectionState: Connected");
                    break;
                case RTCIceConnectionState.Disconnected:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceConnectionState: Disconnected");
                    break;
                case RTCIceConnectionState.Failed:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceConnectionState: Failed");
                    break;
                case RTCIceConnectionState.Max:
                    Debug.Log(THIS_NAME + $"{m_stream} : IceConnectionState: Max");
                    break;
                default:
                    break;
            }
        }

        public void Pause(bool active)
        {
#if false
            foreach (var transceiver in m_pc.GetTransceivers())
                transceiver.Sender.Track.Enabled = active;
#endif

            // https://stackoverflow.com/a/77364499/22575350

            foreach (var transceiver in m_pc.GetTransceivers())
            {
                var param = transceiver.Sender.GetParameters();
                param.encodings[0].active = active;
                transceiver.Sender.SetParameters(param);
            }
        }

        public RTCRtpCodecCapability[] GetAudioCodecs()
        {
            var audioCodecs = new List<RTCRtpCodecCapability>();
            var excludeCodecTypes = new[] { "audio/CN", "audio/telephone-event" };
            foreach (var codec in RTCRtpSender.GetCapabilities(TrackKind.Audio).codecs)
            {
                if (excludeCodecTypes.Count(type => codec.mimeType.Contains(type)) > 0)
                    continue;

                audioCodecs.Add(codec);
            }

            return audioCodecs.ToArray();
        }

        public void InitAudioSender(AudioSource audioSource)
        {
            var track = new AudioStreamTrack(audioSource);

            var sender = m_pc.AddTrack(track, m_sMediaStream);

            var transceiver = m_pc.GetTransceivers().First(t => t.Sender == sender);
            transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;
            var errorType = transceiver.SetCodecPreferences(GetAudioCodecs());
            if (errorType != RTCErrorType.None)
            {
                Debug.LogError(THIS_NAME + $"SetCodecPreferences Error: {errorType}");
            }
        }

        public void InitVideoSender(Texture2D videoSource)
        {
            // TODO:

#if false
                var track = new VideoStreamTrack(videoSource);
                m_pc.AddTrack(track, m_sMediaStream);
#endif
        }

        public void InitReceiver(TrackKind trackKind)
        {
            var transceiver = m_pc.AddTransceiver(trackKind);
            transceiver.Direction = RTCRtpTransceiverDirection.RecvOnly;
        }

        private void OnSetLocalSuccess()
        {
            if (m_iceExchangeOption == IceExchangeOption.Triccle)
            {
                CancelSignalingTask();
                m_signalingTask = m_mono.StartCoroutine(ExchangeSDP());
            }
        }

        private void OnCreateSessionDescriptionError(RTCError error)
        {
            Debug.LogError(THIS_NAME + $"Filed to create Session Description: {error.message}");
        }

        private void OnSetSessionDescriptionError(ref RTCError error)
        {
            Debug.LogError(THIS_NAME + $"Filed to set Sesstion Description: {error.message}");
        }

        private RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new RTCIceServer[]
            {
                new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
            };

            return config;
        }

        private void DisposeMediaTrack()
        {
            m_sMediaStream?.Dispose();
            m_sMediaStream = null;
            m_rMediaStream?.Dispose();
            m_rMediaStream = null;
        }

        public override Task HangUp()
        {
            if (m_dc != null)
            {
                m_dc.Close();
                m_dc = null;
            }

            if (m_pc != null)
            {
                foreach (var transceiver in m_pc.GetTransceivers())
                {
                    if (transceiver.Sender != null && transceiver.Sender.Track != null)
                    {
                        transceiver.Sender.Track.Stop();
                        m_sMediaStream?.RemoveTrack(transceiver.Sender.Track);
                        transceiver.Sender.Track.Dispose();
                    }

                    if (transceiver.Receiver != null && transceiver.Receiver.Track != null)
                    {
                        transceiver.Receiver.Track.Stop();
                        m_rMediaStream?.RemoveTrack(transceiver.Receiver.Track);
                        transceiver.Receiver.Track.Dispose();
                    }
                }

                m_pc.Close();
                m_pc = null;
            }

            DisposeMediaTrack();

            return base.HangUp();
        }

        public void CreatePeerConnection(ClientType clientType, RTCDataChannelInit dataChannelCnf)
        {
            m_clientType = clientType;

            if (m_pc != null)
                HangUp();

            m_sMediaStream = new MediaStream();
            m_rMediaStream = new MediaStream();
            m_rMediaStream.OnAddTrack += (eventTrack) => m_onAddTrack.Invoke(eventTrack);

            var configuration = GetSelectedSdpSemantics();
            m_pc = new RTCPeerConnection(ref configuration);
            m_pc.OnConnectionStateChange = state => OnConnectionStateChange(state);
            m_pc.OnIceCandidate = candidate => OnIceCandidate(candidate);
            m_pc.OnIceConnectionChange = state => OnIceConnectionChange(state);
            m_pc.OnIceGatheringStateChange = state => OnIceGatheringStateChange(state);
            m_pc.OnTrack = eventTrack => m_rMediaStream.AddTrack(eventTrack.Track);

            if (dataChannelCnf != null)
            {
                m_dc = m_pc.CreateDataChannel("data", dataChannelCnf);
                m_dc.OnMessage = OnPacket;
                m_dc.OnOpen = () => Debug.Log(THIS_NAME + "DataChannel Open");
                m_dc.OnClose = () => Debug.Log(THIS_NAME + "DataChannel Close");
            }
        }

        private IEnumerator ExchangeSDP()
        {
            var offer = m_pc.LocalDescription.sdp;
            var stream = m_stream;
            var base64 = "";
            var action = "";

            base64 = Http.GetBase64(new StreamRequest(m_adapter.GetRequestAuth(), stream, offer).ToJson());

            switch (m_clientType)
            {
                case ClientType.Whip:
                    action = "whip";
                    break;
                case ClientType.Whep:
                    action = "whep";
                    break;
            }

            var url = "ws://" + m_adapter.config.GetHostPort() + $"/stream/{action}/{base64}/";

            m_signalingSocket = new WebSocket(url);
            m_signalingSocket.OnOpen += () => Debug.Log("[Signaling] Open!");
            m_signalingSocket.OnError += (e) => Debug.LogError("[Signaling] Error! " + e);
            m_signalingSocket.OnClose += (e) =>
            {
                CancelSignalingTask();
                Debug.Log("[Signaling] Closed !");
            };
            m_signalingSocket.OnMessage += (bytes) =>
            {
                var json = Encoding.UTF8.GetString(bytes);
                var signaling = new Signaling(json);

                if (signaling.isCandidate && signaling.candidate != "")
                {
                    Debug.Log("[Signaling] recv: " + signaling.candidate);

                    m_pc.AddIceCandidate(new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = signaling.candidate,
                        sdpMLineIndex = 0,
                    }));
                }
                else
                {
                    var desc = new RTCSessionDescription
                    {
                        sdp = signaling.sdp,
                        type = RTCSdpType.Answer,
                    };

                    var op2 = m_pc.SetRemoteDescription(ref desc);
                    if (op2.IsError)
                    {
                        var error = op2.Error;
                        OnSetSessionDescriptionError(ref error);
                    }

                    Debug.Log("[Signaling] answer: " + desc.sdp);
                }
            };
            _ = m_signalingSocket.Connect();

            var keepAlive = true;
            while (keepAlive)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                m_signalingSocket?.DispatchMessageQueue();
#endif

                switch (m_pc.ConnectionState)
                {
                    case RTCPeerConnectionState.New:
                    case RTCPeerConnectionState.Connecting:
                        break;
                    default:
                        keepAlive = false;
                        break;
                }

                while (m_candidates.Count > 0)
                {
                    var candidate = m_candidates.Dequeue();
                    var signaling = new Signaling(true, "", "", candidate.Candidate);
                    var json = signaling.ToJson();
                    _ = m_signalingSocket?.SendText(json);

                    Debug.Log("[Signaling] send: " + candidate.Candidate);
                    yield return new WaitForSeconds(0.5f);
                }

                yield return new WaitForSeconds(0.5f);
            }

            m_signalingTask = null;
        }

        private IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
        {
            var op = m_pc.SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess();
            }
            else
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }

        public IEnumerator CreateOffer()
        {
            var op = m_pc.CreateOffer();
            yield return op;

            if (!op.IsError)
            {
                yield return OnCreateOfferSuccess(op.Desc);
            }
            else
            {
                OnCreateSessionDescriptionError(op.Error);
            }
        }

        public unsafe override Task Send(int to, byte[] bytes)
        {
            if ((m_dc != null) && (m_dc.ReadyState == RTCDataChannelState.Open))
            {
                UnsafeUtility.Copy(to, bytes);
                m_dc.Send(bytes);
            }

            return base.Send(to, bytes);
        }

        public override Task Send(int to, string text) => Send(to, Encoding.UTF8.GetBytes(text));
    }
}