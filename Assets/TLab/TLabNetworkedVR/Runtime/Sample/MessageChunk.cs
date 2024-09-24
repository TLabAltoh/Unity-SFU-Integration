using UnityEngine;
using TMPro;

namespace TLab.NetworkedVR.Sample
{
    public class MessageChunk : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_message;
        [SerializeField] private RectTransform m_rect;

        public void InitMessage(string message)
        {
            m_message.text = message;

            m_rect.sizeDelta = new Vector2(m_rect.sizeDelta.x, m_message.preferredHeight);
        }
    }
}
