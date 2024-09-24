using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.NetworkedVR.Network.WebRTC
{
    public class WebRTCClient
    {
        private RTCPeerConnection m_pc;

        private RTCDataChannel m_dataChannel;
        private UnityEvent<string> m_onResponse;
        private UnityEvent<byte[]> m_onDataChannelMessage;

        private MediaStream m_sMediaStream;
        private MediaStream m_rMediaStream;

        private string m_stream;
        private ClientType m_clientType;
        private IceExchangeOption m_iceExchangeOption;

        private MonoBehaviour m_mono;
        private Adapter m_adapter;

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
            OPEN_CHANNEL
        };

        [System.Serializable]
        public class StreamRequest
        {
            public int room_id;
            public string room_pass;
            public int user_id;
            public uint user_token;
            public string stream;
            public string offer;
        }

        #endregion STRUCT

        public enum IceExchangeOption
        {
            TRICCLE,
            VANILLA,
        };

        public static WebRTCClient OpenChannel(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, UnityEvent<string> onResponse, UnityEvent<byte[]> onDataChannelMessage, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            var client = new WebRTCClient(adapter, stream, onResponse, onDataChannelMessage, mono, iceExchangeOption);

            client.CreatePeerConnection(ClientType.OPEN_CHANNEL, dataChannelCnf);

            mono.StartCoroutine(client.CreateOffer());

            return client;
        }

        public static WebRTCClient OpenChannel(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, UnityAction<string> onResponse, UnityAction<byte[]> onDataChannelMessage, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return OpenChannel(mono, adapter, stream, dataChannelCnf, CreateOnResponse(onResponse), CreateOnDataChannelMessage(onDataChannelMessage), iceExchangeOption);
        }

        public static WebRTCClient OpenChannel(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return OpenChannel(mono, adapter, stream, dataChannelCnf, CreateOnResponse(null), CreateOnDataChannelMessage(null), iceExchangeOption);
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, UnityEvent<string> onResponse, UnityEvent<byte[]> onDataChannelMessage, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            var client = new WebRTCClient(adapter, stream, onResponse, onDataChannelMessage, mono, iceExchangeOption);

            client.CreatePeerConnection(ClientType.WHIP, dataChannelCnf);

            if (audioSource)
            {
                client.InitAudioSender(audioSource);
            }

            if (videoSource)
            {
                client.InitVideoSender(videoSource);
            }

            mono.StartCoroutine(client.CreateOffer());

            return client;
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, UnityAction<string> onResponse, UnityAction<byte[]> onDataChannelMessage, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return Whip(mono, adapter, stream, dataChannelCnf, videoSource, audioSource, CreateOnResponse(onResponse), CreateOnDataChannelMessage(onDataChannelMessage), iceExchangeOption);
        }

        public static WebRTCClient Whip(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, Texture2D videoSource, AudioSource audioSource, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return Whip(mono, adapter, stream, dataChannelCnf, videoSource, audioSource, CreateOnResponse(null), CreateOnDataChannelMessage(null), iceExchangeOption);
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, UnityEvent<string> onResponse, UnityEvent<byte[]> onDataChannelMessage, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            var client = new WebRTCClient(adapter, stream, onResponse, onDataChannelMessage, mono, iceExchangeOption);

            client.CreatePeerConnection(ClientType.WHEP, dataChannelCnf);

            if (receiveAudio)
            {
                client.InitAudioReceiver();
            }

            if (receiveVideo)
            {
                client.InitVideReceiver();
            }

            mono.StartCoroutine(client.CreateOffer());

            return client;
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, UnityAction<string> onResponse, UnityAction<byte[]> onDataChannelMessage, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return Whep(mono, adapter, stream, dataChannelCnf, receiveVideo, receiveAudio, CreateOnResponse(onResponse), CreateOnDataChannelMessage(onDataChannelMessage), iceExchangeOption);
        }

        public static WebRTCClient Whep(MonoBehaviour mono, Adapter adapter, string stream, RTCDataChannelInit dataChannelCnf, bool receiveVideo, bool receiveAudio, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            return Whep(mono, adapter, stream, dataChannelCnf, receiveVideo, receiveAudio, CreateOnResponse(null), CreateOnDataChannelMessage(null), iceExchangeOption);
        }

        public static UnityEvent<string> CreateOnResponse(params UnityAction<string>[] @actions)
        {
            var @event = new UnityEvent<string>();
            foreach (var @action in @actions)
            {
                @event.AddListener(@action);
            }

            return @event;
        }

        public static UnityEvent<byte[]> CreateOnDataChannelMessage(params UnityAction<byte[]>[] @actions)
        {
            var @event = new UnityEvent<byte[]>();
            foreach (var @action in @actions)
            {
                @event.AddListener(@action);
            }

            return @event;
        }

        public WebRTCClient(Adapter adapter, string stream, UnityEvent<string> onResponse, UnityEvent<byte[]> onDataChannelMessage, MonoBehaviour mono, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE)
        {
            m_stream = stream;

            m_mono = mono;
            m_adapter = adapter;

            m_iceExchangeOption = iceExchangeOption;

            m_onResponse = onResponse;
            m_onDataChannelMessage = onDataChannelMessage;
        }

        public WebRTCClient(Adapter adapter, string stream, UnityAction<string> onResponse, UnityAction<byte[]> onDataChannelMessage, MonoBehaviour mono, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE) :
            this(adapter, stream, CreateOnResponse(onResponse), CreateOnDataChannelMessage(onDataChannelMessage), mono, iceExchangeOption)
        {

        }

        public WebRTCClient(Adapter adapter, string stream, MonoBehaviour mono, IceExchangeOption iceExchangeOption = IceExchangeOption.TRICCLE) :
            this(adapter, stream, CreateOnResponse(null), CreateOnDataChannelMessage(null), mono, iceExchangeOption)
        {

        }

        public void SetCallback(UnityAction<byte[]> callback)
        {
            m_onDataChannelMessage.AddListener(callback);
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {

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
                        m_mono.StartCoroutine(ExchangeSDF());
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
                    Debug.Log(THIS_NAME + "IceConnectionState: Connected");
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

        public void OnPauseAudio(bool active)
        {
            foreach (var transceiver in m_pc.GetTransceivers())
            {
                transceiver.Sender.Track.Enabled = active;
            }
        }

        public void OnPauseVideo(bool active)
        {
            // TODO
        }

        public RTCRtpCodecCapability[] GetAudioCodecs()
        {
            var audioCodecs = new List<RTCRtpCodecCapability>();
            var excludeCodecTypes = new[] { "audio/CN", "audio/telephone-event" };
            foreach (var codec in RTCRtpSender.GetCapabilities(TrackKind.Audio).codecs)
            {
                if (excludeCodecTypes.Count(type => codec.mimeType.Contains(type)) > 0)
                {
                    continue;
                }

                audioCodecs.Add(codec);
            }

            return audioCodecs.ToArray();
        }

        public void InitAudioSender(AudioSource audioSource)
        {
            var track = new AudioStreamTrack(audioSource);

            // One transceiver is added at this timing, so AddTransceiver does not need to be executed.
            var sender = m_pc.AddTrack(track, m_sMediaStream);

            var transceiver = m_pc.GetTransceivers().First(t => t.Sender == sender);
            transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;
            var errorType = transceiver.SetCodecPreferences(GetAudioCodecs());
            if (errorType != RTCErrorType.None)
            {
                Debug.LogError(THIS_NAME + $"SetCodecPreferences Error: {errorType}");
            }
        }

        public void InitAudioReceiver()
        {
            m_pc.AddTransceiver(TrackKind.Audio);
        }

        public void InitVideoSender(Texture2D videoSource)
        {
            // TODO:

#if false
            if (m_streamVideoOnConnect)
            {
                var track = new VideoStreamTrack(m_videoStreamSrc);

                m_pcDic[dst].AddTrack(track, m_sMediaStream);
            }
#endif
        }

        public void InitVideReceiver()
        {
            m_pc.AddTransceiver(TrackKind.Video);
        }

        private void OnSetLocalSuccess(RTCPeerConnection pc)
        {
            if (m_iceExchangeOption == IceExchangeOption.TRICCLE)
            {
                m_mono.StartCoroutine(ExchangeSDF());
            }
        }

        private void OnSetRemoteSuccess(RTCPeerConnection pc)
        {
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

        public void HangUpDataChannel()
        {
            if (m_dataChannel != null)
            {
                m_dataChannel.Close();
                m_dataChannel = null;
            }

            if (m_pc != null)
            {
                foreach (var transceiver in m_pc.GetTransceivers())
                {
                    if (transceiver.Sender != null)
                    {
                        transceiver.Sender.Track.Stop();
                        m_sMediaStream.RemoveTrack(transceiver.Sender.Track);
                        transceiver.Sender.Track.Dispose();
                    }

                    if (transceiver.Receiver != null)
                    {
                        transceiver.Receiver.Track.Stop();
                        m_rMediaStream.RemoveTrack(transceiver.Receiver.Track);
                        transceiver.Receiver.Track.Dispose();
                    }
                }

                m_pc.Close();
                m_pc = null;
            }
        }

        public void CreatePeerConnection(ClientType clientType, RTCDataChannelInit dataChannelCnf)
        {
            m_clientType = clientType;

            if (m_pc != null)
            {
                HangUpDataChannel();
            }

            var configuration = GetSelectedSdpSemantics();
            m_pc = new RTCPeerConnection(ref configuration);
            m_pc.OnIceCandidate = candidate => {
                OnIceCandidate(candidate);
            };
            m_pc.OnIceConnectionChange = state => {
                OnIceConnectionChange(state);
            };
            m_pc.OnIceGatheringStateChange = state =>
            {
                OnIceGatheringStateChange(state);
            };
            m_pc.OnTrack = eventTrack => {
                m_rMediaStream.AddTrack(eventTrack.Track);
            };

            if (dataChannelCnf != null)
            {
                m_dataChannel = m_pc.CreateDataChannel("data", dataChannelCnf);
                m_dataChannel.OnMessage = bytes => {
                    m_onDataChannelMessage.Invoke(bytes);
                };
                m_dataChannel.OnOpen = () => {
                    Debug.Log(THIS_NAME + "DataChannel Open");
                };
                m_dataChannel.OnClose = () => {
                    Debug.Log(THIS_NAME + "DataChannel Close");
                };
            }
        }

        private IEnumerator OnAnswer(RTCSessionDescription desc)
        {
            var op2 = m_pc.SetRemoteDescription(ref desc);
            yield return op2;

            if (!op2.IsError)
            {
                OnSetRemoteSuccess(m_pc);
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }

            yield break;
        }

        private IEnumerator ExchangeSDF()
        {
            var offer = m_pc.LocalDescription.sdp;
            var stream = m_stream;
            var base64 = "";
            var action = "";

            var @object = new StreamRequest
            {
                room_id = m_adapter.room.id,
                room_pass = m_adapter.room.config.createOffer.room_pass,
                user_id = m_adapter.user.id,
                user_token = m_adapter.user.token,
                stream = stream,
                offer = offer,
            };

            base64 = Http.GetBase64(@object);

            switch (m_clientType)
            {
                case ClientType.WHIP:
                    action = "whip";
                    break;
                case ClientType.WHEP:
                    action = "whep";
                    break;
                case ClientType.OPEN_CHANNEL:
                    action = "open_channel";
                    break;
            }

            var url = m_adapter.room.config.address + $"/stream/{action}/{base64}/";

            var task = System.Threading.Tasks.Task.Run(async () =>
            {
                System.Uri uri = new System.UriBuilder(url).Uri;

                var client = new System.Net.Http.HttpClient();
                var res = await client.PostAsync(uri, null);
                res.EnsureSuccessStatusCode();

                string data = await res.Content.ReadAsStringAsync();
                return data;
            });

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Exchange SDP failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var remoteDescription = new RTCSessionDescription
            {
                sdp = task.Result,
                type = RTCSdpType.Answer,
            };

            yield return OnAnswer(remoteDescription);
        }

        private IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
        {
            var op = m_pc.SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess(m_pc);
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

        public void DataChannelSend(byte[] bytes)
        {
            if (m_dataChannel != null)
            {
                if (m_dataChannel.ReadyState == RTCDataChannelState.Open)
                {
                    m_dataChannel.Send(bytes);
                }
            }
        }
    }
}