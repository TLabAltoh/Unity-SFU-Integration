using UnityEngine;
using TMPro;

namespace TLab.SFU.UI
{
    [AddComponentMenu("TLab/SFU/Log Chunk (TLab)")]
    public class LogChunk : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_message;
        [SerializeField] private RectTransform m_rect;

        public void Init(string message)
        {
            m_message.text = message;

            m_rect.sizeDelta = new Vector2((m_rect.parent as RectTransform).rect.width, m_message.preferredHeight);
        }
    }
}
