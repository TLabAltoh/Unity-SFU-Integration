using UnityEngine;
using UnityEngine.UI;

namespace TLab.SFU.UI
{
    [AddComponentMenu("TLab/SFU/Log View (TLab)")]
    public class LogView : MonoBehaviour
    {
        [SerializeField] private ScrollRect m_scrollRect;

        private Transform m_scrollViewContent;

        private bool m_scrollViewAvailable = false;
        private bool m_forceScrollToTail = true;

        private string THIS_NAME => "[" + GetType() + "] ";

        public void Append(string message)
        {
            if (m_scrollViewAvailable)
            {
                var messageChunk = Instantiate(Resources.Load<GameObject>("UI/LogChunk"));

                messageChunk.transform.SetParent(m_scrollViewContent);
                messageChunk.GetComponent<LogChunk>()?.Init(message);
            }
            else
                Debug.Log(THIS_NAME + message);
        }

        private void Start()
        {
            if (m_scrollRect == null)
                return;

            m_scrollViewContent = m_scrollRect.transform.Find("Viewport/Content");

            m_scrollRect.onValueChanged.AddListener((value) =>
            {
                if (UnityEngine.Input.GetMouseButton(0))
                    m_forceScrollToTail = (value.y < 0.1f);
            });

            m_scrollViewAvailable = true;
        }

        private void Update()
        {
            if (m_scrollViewAvailable && m_forceScrollToTail && !UnityEngine.Input.GetMouseButton(0))
                m_scrollRect.verticalNormalizedPosition = 0.0f;
        }
    }
}