using System.Text;
using UnityEngine;
using Unity.WebRTC;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    [RequireComponent(typeof(AudioSource))]
    public class WebClientSample : ClientSample, ISfuClientEventHandler
    {
        private SfuClient m_client;

        private bool m_useWebRTC = true;
        private bool m_useAudio = true;

        private AudioSource m_audioSource;

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public override void Open()
        {
            if (m_useWebRTC)
            {
                if (m_useAudio && m_adapter.userId == 0)
                {
                    StartAudio();
                    m_client = WebRTCClient.Whip(this, m_adapter, STREAM, OnMessage, (OnOpen, OnOpen), (OnClose, OnClose), OnError, new RTCDataChannelInit(), null, m_audioSource);
                }
                else
                    m_client = WebRTCClient.Whep(this, m_adapter, STREAM, OnMessage, (OnOpen, OnOpen), (OnClose, OnClose), OnError, new RTCDataChannelInit(), false, m_useAudio, OnAddTrack);
            }
            else
                m_client = WebSocketClient.Open(this, m_adapter, STREAM, OnMessage, (OnOpen, OnOpen), (OnClose, OnClose), OnError);
        }

        private void OnAddTrack(MediaStreamTrackEvent e)
        {
            Debug.Log(THIS_NAME + "Receive track: " + e.Track.Id);

            var track = e.Track as AudioStreamTrack;
            m_audioSource.SetTrack(track);
            m_audioSource.loop = true;
            m_audioSource.Play();
        }

        private void StartAudio()
        {
            var clip = Resources.Load<AudioClip>("TLab/SFU/Sample/sin_mono");
            if (!clip)
                Debug.LogError(THIS_NAME + "Audio clip is null !");
            m_audioSource.loop = true;
            m_audioSource.clip = clip;
            m_audioSource.Play();
        }

        public override void Close() => m_client?.HangUp();

        public override void SendText(string message) => m_client.Send(m_adapter.userId, Encoding.UTF8.GetBytes(message));

        public override void Send(byte[] bytes) => m_client.Send(m_adapter.userId, bytes);

        public void OnMessage(int from, int to, byte[] bytes) => m_logView?.Append(Encoding.UTF8.GetString(bytes, SfuClient.RECV_PACKET_HEADER_SIZE, bytes.Length - SfuClient.RECV_PACKET_HEADER_SIZE));

        public void OnOpen() => Debug.Log(THIS_NAME + "Open");

        public void OnClose() => Debug.Log(THIS_NAME + "Close");

        public void OnOpen(int from) => Debug.Log(THIS_NAME + "Open: " + from);

        public void OnClose(int from) => Debug.Log(THIS_NAME + "Close: " + from);

        public void OnError()
        {
            Debug.Log(THIS_NAME + "Error");
            m_client?.HangUp();
        }

        protected override void Start()
        {
            base.Start();

            m_audioSource = GetComponent<AudioSource>();
        }
    }
}
