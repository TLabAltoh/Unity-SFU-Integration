using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;
using static System.BitConverter;

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

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        #region STRUCT

        public enum ClientType
        {
            WHIP,
            WHEP,
        };

        [System.Serializable]
        public class Signaling
        {
            public bool is_candidate;
            public string sdp;
            public string session;
            public string candidate;

            public Signaling() { }

            public Signaling(bool is_candidate, string sdp, string session, string candidate)
            {
                this.is_candidate = is_candidate;
                this.sdp = sdp;
                this.session = session;
                this.candidate = candidate;
            }
        };

        [System.Serializable]
        public class StreamRequest : RequestAuth
        {
            public string stream;
            public string offer;

            public StreamRequest(RequestAuth auth, string stream, string offer) : base(auth)
            {
                this.stream = stream;
                this.offer = offer;
            }
        }

        #endregion STRUCT

        public enum IceExchangeOption
        {
            TRICCLE,
            VANILLA,
        };

        public static UnityEvent<MediaStreamTrackEvent> CreateOnAddTrack(params UnityAction<MediaStreamTrackEvent>[] @actions)
        {
            var @event = new UnityEvent<MediaStreamTrackEvent>();
            foreach (var @action in @actions)
                @event.AddListener(@action);
            return @event;
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onReceive, UnityEvent<int> onConnect, UnityEvent<int> onDisconnect, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, UnityEvent<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            var client = new WebRTCClient(mono, adapter, stream, onReceive, onConnect, onDisconnect, onAddTrack, iceExchangeOption);

            client.CreatePeerConnection(ClientType.WHIP, dataChannelCnf);

            if (audioSource)
                client.InitAudioSender(audioSource);

            if (videoSource)
                client.InitVideoSender(videoSource);

            mono.StartCoroutine(client.CreateOffer());

            return client;
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onReceive, UnityAction<int> onConnect, UnityAction<int> onDisconnect, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, UnityAction<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return Whip(mono, adapter, stream, CreateEvent(onReceive), CreateEvent(onConnect), CreateEvent(onDisconnect), dataChannelCnf, videoSource, audioSource, CreateOnAddTrack(onAddTrack), iceExchangeOption);
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return Whip(mono, adapter, stream, CreateEvent<int, int, byte[]>(null), CreateEvent<int>(null), CreateEvent<int>(null), dataChannelCnf, videoSource, audioSource, CreateOnAddTrack(null), iceExchangeOption);
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onReceive, UnityEvent<int> onConnect, UnityEvent<int> onDisconnect, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, UnityEvent<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            var client = new WebRTCClient(mono, adapter, stream, onReceive, onConnect, onDisconnect, onAddTrack, iceExchangeOption);

            client.CreatePeerConnection(ClientType.WHEP, dataChannelCnf);

            if (receiveAudio)
                client.InitReceiver(TrackKind.Audio);

            if (receiveVideo)
                client.InitReceiver(TrackKind.Video);

            mono.StartCoroutine(client.CreateOffer());

            return client;
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onReceive, UnityAction<int> onConnect, UnityAction<int> onDisconnect, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, UnityAction<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return Whep(mono, adapter, stream, CreateEvent(onReceive), CreateEvent(onConnect), CreateEvent(onDisconnect), dataChannelCnf, receiveVideo, receiveAudio, CreateOnAddTrack(onAddTrack), iceExchangeOption);
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return Whep(mono, adapter, stream, CreateEvent<int, int, byte[]>(null), CreateEvent<int>(null), CreateEvent<int>(null), dataChannelCnf, receiveVideo, receiveAudio, CreateOnAddTrack(null), iceExchangeOption);
        }

        public WebRTCClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onReceive, UnityEvent<int> onConnect, UnityEvent<int> onDisconnect, UnityEvent<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE) : base(mono, adapter, stream, onReceive, onConnect, onDisconnect)
        {
            m_iceExchangeOption = iceExchangeOption;
            m_onAddTrack = onAddTrack;
        }

        public WebRTCClient(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onReceive, UnityAction<int> onConnect, UnityAction<int> onDisconnect, UnityAction<MediaStreamTrackEvent> onAddTrack, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE) :
            this(mono, adapter, stream, CreateEvent(onReceive), CreateEvent(onConnect), CreateEvent(onDisconnect), CreateOnAddTrack(onAddTrack), iceExchangeOption)
        {

        }

        public WebRTCClient(MonoBehaviour mono, Adapter adapter, string stream, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE) :
            this(mono, adapter, stream, CreateEvent<int, int, byte[]>(null), CreateEvent<int>(null), CreateEvent<int>(null), CreateOnAddTrack(null), iceExchangeOption)
        {

        }

        public void SetCallback(UnityAction<int, int, byte[]> callback)
        {
            m_onReceive.AddListener(callback);
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            m_candidates.Enqueue(candidate);
        }

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

        private void OnIceGatheringStateChange(RTCIceGatheringState state)
        {
            switch (state)
            {
                case RTCIceGatheringState.New:
                    Debug.Log(THIS_NAME + "IceGatheringState: New");
                    break;
                case RTCIceGatheringState.Gathering:
                    Debug.Log(THIS_NAME + "IceGatheringState: Gathering");
                    break;
                case RTCIceGatheringState.Complete:
                    Debug.Log(THIS_NAME + "IceGatheringState: Complete");
                    if (m_iceExchangeOption == IceExchangeOption.VANILLA)
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
                    Debug.Log(THIS_NAME + "IceConnectionState: New");
                    break;
                case RTCIceConnectionState.Checking:
                    Debug.Log(THIS_NAME + "IceConnectionState: Checking");
                    break;
                case RTCIceConnectionState.Closed:
                    Debug.Log(THIS_NAME + "IceConnectionState: Closed");
                    break;
                case RTCIceConnectionState.Completed:
                    Debug.Log(THIS_NAME + "IceConnectionState: Completed");
                    break;
                case RTCIceConnectionState.Connected:
                    break;
                case RTCIceConnectionState.Disconnected:
                    Debug.Log(THIS_NAME + "IceConnectionState: Disconnected");
                    break;
                case RTCIceConnectionState.Failed:
                    Debug.Log(THIS_NAME + "IceConnectionState: Failed");
                    break;
                case RTCIceConnectionState.Max:
                    Debug.Log(THIS_NAME + "IceConnectionState: Max");
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
            if (m_iceExchangeOption == IceExchangeOption.TRICCLE)
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
            m_pc.OnIceCandidate = candidate => OnIceCandidate(candidate);
            m_pc.OnIceConnectionChange = state => OnIceConnectionChange(state);
            m_pc.OnIceGatheringStateChange = state => OnIceGatheringStateChange(state);
            m_pc.OnTrack = eventTrack => m_rMediaStream.AddTrack(eventTrack.Track);

            if (dataChannelCnf != null)
            {
                m_dc = m_pc.CreateDataChannel("data", dataChannelCnf);
                m_dc.OnMessage = bytes => OnPacket(bytes);
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

            base64 = Http.GetBase64(new StreamRequest(m_adapter.GetRequestAuth(), stream, offer));

            switch (m_clientType)
            {
                case ClientType.WHIP:
                    action = "whip";
                    break;
                case ClientType.WHEP:
                    action = "whep";
                    break;
            }

            var url = "ws://" + m_adapter.room.config.GetHostPort() + $"/stream/{action}/{base64}/";

            m_signalingSocket = new WebSocket(url);
            m_signalingSocket.OnOpen += () => Debug.Log("[Signaling] Connection open!");
            m_signalingSocket.OnError += (e) => Debug.Log("[Signaling] Error! " + e);
            m_signalingSocket.OnClose += (e) =>
            {
                CancelSignalingTask();
                Debug.Log("[Signaling] Connection closed!");
            };
            m_signalingSocket.OnMessage += (bytes) =>
            {
                var json = Encoding.UTF8.GetString(bytes);
                var signaling = JsonUtility.FromJson<Signaling>(json);

                if (signaling.is_candidate)
                {
                    m_pc.AddIceCandidate(new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = signaling.candidate,
                        sdpMLineIndex = 0,
                    }));

                    Debug.Log("[Signaling] " + signaling.candidate);
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

                    Debug.Log("[Signaling] Answer received: " + desc.sdp);
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
                    var json = JsonUtility.ToJson(signaling);
                    _ = m_signalingSocket?.SendText(json);

                    Debug.Log("[Signaling] Send candidate: " + candidate.Candidate);
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

        public override Task Send(int to, byte[] bytes)
        {
            if ((m_dc != null) && (m_dc.ReadyState == RTCDataChannelState.Open))
            {
                var hedder = GetBytes(to);
                var packet = hedder.Concat(bytes);
                m_dc.Send(packet.ToArray());
            }

            return base.Send(to, bytes);
        }

        public override Task SendText(int to, string text)
        {
            return Send(to, Encoding.UTF8.GetBytes(text));
        }
    }
}