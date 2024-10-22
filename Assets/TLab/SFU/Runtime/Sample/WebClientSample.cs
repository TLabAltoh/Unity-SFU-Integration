using System.Text;
using UnityEngine;
using Unity.WebRTC;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    [RequireComponent(typeof(AudioSource))]
    public class WebClientSample : ClientSample
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
                if (m_useAudio && m_adapter.user.id == 0)
                {
                    StartAudio();
                    m_client = WebRTCClient.Whip(this, m_adapter, STREAM, OnReceive, OnConnect, OnDisconnect, new RTCDataChannelInit(), null, m_audioSource, null);
                }
                else
                {
                    m_client = WebRTCClient.Whep(this, m_adapter, STREAM, OnReceive, OnConnect, OnDisconnect, new RTCDataChannelInit(), false, m_useAudio, OnAddTrack);
                }
            }
            else
            {
                m_client = WebSocketClient.Open(this, m_adapter, STREAM, OnReceive, OnConnect, OnDisconnect);
            }
        }

        private void OnAddTrack(MediaStreamTrackEvent e)
        {
            Debug.Log(THIS_NAME + "Receive track: " + e.Track.Id);
        }

        private void StartAudio()
        {
            var clip = Resources.Load<AudioClip>("Sample/sin_mono");
            if (!clip)
                Debug.LogError(THIS_NAME + "Audio clip is null !");
            m_audioSource.loop = true;
            m_audioSource.clip = clip;
            m_audioSource.Play();
        }

        public override void Close() => m_client?.HangUp();

        public override void SendText(string message) => m_client.Send(m_adapter.user.id, Encoding.UTF8.GetBytes(message));

        public override void Send(byte[] bytes) => m_client.Send(m_adapter.user.id, bytes);

        public void OnReceive(int from, int to, byte[] bytes) => OnMessage(Encoding.UTF8.GetString(bytes));

        public void OnConnect(int from) => Debug.Log(THIS_NAME + "Connect: " + from);

        public void OnDisconnect(int from) => Debug.Log(THIS_NAME + "Disconnect: " + from);

        protected override void Start()
        {
            base.Start();

            m_audioSource = GetComponent<AudioSource>();
        }
    }
}
