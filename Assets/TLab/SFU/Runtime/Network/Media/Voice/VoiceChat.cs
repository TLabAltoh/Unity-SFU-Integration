using System.Collections;
using UnityEngine;
using Unity.WebRTC;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Voice Chat (TLab)")]
    [RequireComponent(typeof(AudioSource))]
    public class VoiceChat : MonoBehaviour
    {
        public static VoiceChat instance;

        private AudioSource m_microphoneSource;
        private AudioClip m_microphoneClip;
        private string m_microphoneName;
        private bool m_recording = false;

        private WebRTCClient m_mediaChannel;

        public const int VOICE_BUFFER_SIZE = 1024;
        public const int CHANNEL = 1;
        public const int LENGTH_SECOUND = 1;

        [HideInInspector] public int m_frequency = 16000;

        private static Hashtable m_voicePlayers = new Hashtable();

        private string THIS_NAME => "[" + GetType().Name + "] ";

        public AudioSource microphoneSource => m_microphoneSource;

        public static void RegistClient(Address64 id, VoiceChatPlayer player)
        {
            m_voicePlayers[id] = player;
        }

        public static void RemoveClient(Address64 id)
        {
            m_voicePlayers.Remove(id);
        }

        public void OnVoice(string userId, AudioStreamTrack track)
        {
            var player = m_voicePlayers[userId] as VoiceChatPlayer;

            if (player == null)
            {
                Debug.LogError(THIS_NAME + $"Player not found: {userId}");
                return;
            }

            player.SetAudioStreamTrack(track);
        }

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

        public void Whep()
        {
            // TODO:
        }

        public void Whip()
        {
            if (m_mediaChannel == null)
            {
                return;
            }

            m_recording = StartRecording();
            if (!m_recording)
            {
                return;
            }

            while (!(Microphone.GetPosition(m_microphoneName) > 0)) { }

            m_microphoneSource.clip = m_microphoneClip;
            m_microphoneSource.loop = true;
            m_microphoneSource.Play();

            WebRTCClient.Whip(this, SyncClient.adapter, "voice", null, null, microphoneSource);
        }

        public void Pause(bool active)
        {
            m_mediaChannel.Pause(active);
        }

        private void InitializeDPSBuffer()
        {
            var configuration = AudioSettings.GetConfiguration();
            configuration.dspBufferSize = VOICE_BUFFER_SIZE;
            if (!AudioSettings.Reset(configuration))
            {
                Debug.LogError(THIS_NAME + "Failed changing Audio Settings");
            }
        }

        private void Awake()
        {
            instance = this;

            InitializeDPSBuffer();
        }

        private void Start()
        {
            m_microphoneSource = GetComponent<AudioSource>();
        }
    }
}
