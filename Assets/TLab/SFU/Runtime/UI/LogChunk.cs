using UnityEngine;
using TMPro;

namespace TLab.SFU.UI
{
    [AddComponentMenu("TLab/SFU/Log Chunk (TLab)")]
    public class LogChunk : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_message;

        public void Init(string message)
        {
            m_message.text = message;

            var rect = transform as RectTransform;
            rect.sizeDelta = new Vector2((rect.parent as RectTransform).rect.size.x, m_message.preferredHeight);
        }
    }
}
