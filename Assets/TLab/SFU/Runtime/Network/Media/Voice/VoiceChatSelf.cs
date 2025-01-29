using UnityEngine;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Voice Chat Self (TLab)")]
    public class VoiceChatSelf : MonoBehaviour
    {
        public void On() => VoiceChat.self.Whip();

        public void Pause(bool active) => VoiceChat.self.Pause(active);
    }
}
