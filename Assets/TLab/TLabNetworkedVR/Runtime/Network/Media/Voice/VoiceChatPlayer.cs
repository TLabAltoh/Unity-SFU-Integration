using UnityEngine;
using Unity.WebRTC;

namespace TLab.NetworkedVR.Network
{
    [AddComponentMenu("TLab/NetworkedVR/Voice Chat Player (TLab)")]
    [RequireComponent(typeof(AudioSource))]
    public class VoiceChatPlayer : NetworkedObject
    {
        private AudioSource m_outputAudioSource;

        public void SetAudioStreamTrack(AudioStreamTrack track)
        {
            m_outputAudioSource.SetTrack(track);
            m_outputAudioSource.loop = true;
            m_outputAudioSource.Play();
        }

        public override void Init()
        {
            base.Init();

            VoiceChat.RegistClient(m_networkedId.id, this);
        }

        public override void Init(string id)
        {
            base.Init(id);

            VoiceChat.RegistClient(m_networkedId.id, this);
        }

        protected override void Start()
        {
            base.Start();

            m_outputAudioSource = GetComponent<AudioSource>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            VoiceChat.RemoveClient(m_networkedId.id);
        }
    }
}
