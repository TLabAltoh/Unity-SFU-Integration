using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;

namespace TLab.SFU.Network
{
    using Registry = Registry<int, VoiceChat>;

    [AddComponentMenu("TLab/SFU/Voice Chat (TLab)")]
    [RequireComponent(typeof(AudioSource))]
    public class VoiceChat : NetworkObject
    {
        [System.Serializable, Message(typeof(MSG_VoiceOpenNortification))]
        public class MSG_VoiceOpenNortification : Message { }

        [SerializeField] private int m_frequency = 16000;

        private AudioSource m_microphoneSource;
        private AudioClip m_microphoneClip;
        private string m_microphoneName;
        private bool m_recording = false;

        private static bool m_dpsBufferInitialized = false;

        private Queue<int> m_requests = new Queue<int>();

        private WebRTCClient m_rtcClient;

        public const int VOICE_BUFFER_SIZE = 1024;
        public const int CHANNEL = 1;
        public const int LENGTH_SECOUND = 1;

        private string THIS_NAME => "[" + GetType().Name + "] ";

        private string GetMicrophone()
        {
            if (Microphone.devices.Length > 0)
                return Microphone.devices[0];

            return null;
        }

        private bool StartRecording()
        {
            m_microphoneName = GetMicrophone();
            if (m_microphoneName == null)
            {
                Debug.LogError(THIS_NAME + "Mic Device is empty");
                return false;
            }

            Microphone.GetDeviceCaps(m_microphoneName, out int minFreq, out m_frequency);
            Debug.Log(THIS_NAME + $"minFreq: {minFreq}, maxFreq: {m_frequency}");

            m_microphoneClip = Microphone.Start(m_microphoneName, true, LENGTH_SECOUND, m_frequency);
            if (m_microphoneClip == null)
            {
                Debug.LogError(THIS_NAME + "Failed to recording, using " + m_microphoneName);
                return false;
            }

            Debug.Log(THIS_NAME + $"sampleRate: {m_microphoneClip.frequency}, channel: {m_microphoneClip.channels}, micName: {m_microphoneName}");

            return true;
        }

        public override void Init(in Address32 @public, bool self)
        {
            base.Init(@public, self);

            m_microphoneSource = GetComponent<AudioSource>();

            if (!self)
                OnSyncRequestComplete(m_group.owner);
            else
                Whip($"stream#voice#{m_group.owner}");
        }

        public void Whep(string stream)
        {
            m_rtcClient = WebRTCClient.Whep(this, NetworkClient.adapter, stream, null, (OnWhepOpen, OnWhepOpen), (OnWhepClose, OnWhepClose), OnError, new RTCDataChannelInit(), false, true, OnTrack);
        }

        public void Whip(string stream)
        {
            m_recording = StartRecording();
            if (!m_recording)
                return;

            while (!(Microphone.GetPosition(m_microphoneName) > 0)) { }

            m_microphoneSource.clip = m_microphoneClip;
            m_microphoneSource.loop = true;
            m_microphoneSource.Play();

            m_rtcClient = WebRTCClient.Whip(this, NetworkClient.adapter, stream, null, (OnWhipOpen, OnWhipOpen), (OnWhipClose, OnWhipClose), OnError, new RTCDataChannelInit(), null, m_microphoneSource);
        }

        private void OnTrack(MediaStreamTrackEvent t)
        {
            Debug.Log($"{THIS_NAME}: OnTrack {t.Track.Id}");
        }

        private void OnWhipOpen()
        {
            Debug.Log($"{THIS_NAME}: {nameof(OnWhipOpen)} !");

            foreach (var user in m_requests)
                NetworkClient.SendWS(new MSG_VoiceOpenNortification().Marshall());

            m_requests.Clear();
        }

        private void OnWhepOpen()
        {
            Debug.Log($"{THIS_NAME}: {nameof(OnWhepOpen)} !");
        }

        private void OnWhipClose()
        {
            Debug.Log($"{THIS_NAME}: {nameof(OnWhipClose)} !");
        }

        private void OnWhepClose()
        {
            Debug.Log($"{THIS_NAME}: {nameof(OnWhepClose)} !");
        }

        private void OnWhipOpen(int from)
        {

        }

        private void OnWhepOpen(int from)
        {

        }

        private void OnWhipClose(int from)
        {

        }

        private void OnWhepClose(int from)
        {

        }

        private void OnError() { }

        public void Pause(bool active) => m_rtcClient?.Pause(active);

        private void InitializeDPSBuffer()
        {
            var cnf = AudioSettings.GetConfiguration();
            cnf.dspBufferSize = VOICE_BUFFER_SIZE;
            if (!AudioSettings.Reset(cnf))
                Debug.LogError(THIS_NAME + $"Failed changing {nameof(AudioSettings)}");
        }

        protected void OnVoiceRequest(int from)
        {
            if (m_rtcClient.connected)
                NetworkClient.SendWS(new MSG_VoiceOpenNortification().Marshall());
            else
                m_requests.Enqueue(from);
        }

        protected void OnVoice(int from) => Whep($"stream#voice#{m_group.owner}");

        public override void OnSyncRequest(int from)
        {
            base.OnSyncRequest(from);

            Registry.GetByKey(NetworkClient.userId)?.OnVoiceRequest(from);

            // No response
        }

        protected override void RegisterOnMessage()
        {
            base.RegisterOnMessage();

            if (!m_dpsBufferInitialized)
            {
                InitializeDPSBuffer();
                m_dpsBufferInitialized = true;
            }

            NetworkClient.RegisterOnMessage<MSG_VoiceOpenNortification>((from, to, bytes) =>
            {
                Registry.GetByKey(to)?.OnVoice(from);
            });
        }

        protected override void Register()
        {
            base.Register();

            Registry.Register(m_group.owner, this);
        }

        protected override void Unregister()
        {
            Registry.Unregister(m_group.owner);

            base.Unregister();
        }
    }
}
