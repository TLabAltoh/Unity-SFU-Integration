using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    public class ClientSample : MonoBehaviour
    {
        private bool m_forceScrollToTail = true;
        private TMP_InputField m_messageInput;
        private ScrollRect m_scrollRect;
        private Transform m_scrollViewContent;

        protected Adapter m_adapter;

        protected const string STREAM = "defualt";

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public void OnMessage(string message)
        {
            var chunk = Instantiate(Resources.Load<GameObject>("Sample/Message"));
            chunk.transform.SetParent(m_scrollViewContent);
            chunk.GetComponent<MessageChunk>()?.InitMessage(message);
        }

        public virtual void Open() { }

        public virtual void Close() { }

        public virtual void SendText(string message) { }

        public virtual void Send(byte[] bytes) { }

        public void Join()
        {
            if (m_adapter == null)
            {
                Debug.LogError(THIS_NAME + "Adapter is NULL");
                return;
            }

            m_adapter.JoinRoom(this, (response) =>
            {
                OnMessage(response);
                Open();
            });
        }

        public void Exit()
        {
            if (m_adapter == null)
            {
                Debug.LogError(THIS_NAME + "Adapter is NULL");
                return;
            }

            Close();

            m_adapter.ExitRoom(this, (response) =>
            {
                OnMessage(response);
            });
        }

        private IEnumerator CloneAdapter()
        {
            while (AdapterSample.state != AdapterSample.State.CONNECTED)
                yield return null;

            m_adapter = AdapterSample.instance.GetClone();
        }

        protected virtual void Start()
        {
            m_messageInput = GetComponentInChildren<TMP_InputField>();

            m_scrollRect = GetComponentInChildren<ScrollRect>();
            m_scrollViewContent = m_scrollRect.transform.Find("Viewport/Content");

            m_scrollRect.onValueChanged.AddListener((value) =>
            {
                if (UnityEngine.Input.GetMouseButton(0))
                    m_forceScrollToTail = (value.y < 0.1f);
            });

            StartCoroutine(CloneAdapter());
        }

        protected virtual void Update()
        {
            if (m_forceScrollToTail && !UnityEngine.Input.GetMouseButton(0))
                m_scrollRect.verticalNormalizedPosition = 0.0f;
        }
    }
}
