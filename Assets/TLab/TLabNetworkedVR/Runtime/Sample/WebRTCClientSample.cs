using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using TMPro;
using TLab.NetworkedVR.Network;
using TLab.NetworkedVR.Network.WebRTC;

namespace TLab.NetworkedVR.Sample
{
    public class WebRTCClientSample : MonoBehaviour
    {
        private WebRTCClient m_client;

        private bool m_forceScrollToTail = true;

        private TMP_InputField m_messageInput;
        private ScrollRect m_scrollRect;
        private Transform m_scrollViewContent;

        private Adapter m_adapter;

        private const string STREAM = "defualt";

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public void Join()
        {
            if (m_adapter == null)
            {
                Debug.LogError(THIS_NAME + "Adapter is NULL");

                return;
            }

            m_adapter.JoinRoom(this, (response) =>
            {
                OnResponse(response);

                m_client = WebRTCClient.OpenChannel(this, m_adapter, STREAM, new RTCDataChannelInit(), OnResponse, OnDataChannelMessage);
            });
        }

        public void Exit()
        {
            if (m_adapter == null)
            {
                Debug.LogError(THIS_NAME + "Adapter is NULL");

                return;
            }

            m_client?.HangUpDataChannel();

            m_adapter.ExitRoom(this, (response) =>
            {
                OnResponse(response);
            });
        }

        public void DataChannelSend(string message)
        {
            m_client.DataChannelSend(Encoding.UTF8.GetBytes(message));
        }

        public void OnMessage(string message)
        {
            var messageChunk = Instantiate(Resources.Load<GameObject>("Sample/Message"));

            messageChunk.transform.SetParent(m_scrollViewContent);
            messageChunk.GetComponent<MessageChunk>()?.InitMessage(message);
        }

        public void OnResponse(string message)
        {
            OnMessage(message);
        }

        public void OnDataChannelMessage(byte[] bytes)
        {
            OnMessage(this.gameObject.name + ": " + Encoding.UTF8.GetString(bytes));
        }

        private IEnumerator CloneAdapter()
        {
            while (AdapterSample.state != AdapterSample.State.CONNECTED)
            {
                yield return null;
            }

            m_adapter = AdapterSample.instance.GetClone();
        }

        private void Start()
        {
            m_messageInput = GetComponentInChildren<TMP_InputField>();

            m_scrollRect = GetComponentInChildren<ScrollRect>();
            m_scrollViewContent = m_scrollRect.transform.Find("Viewport/Content");

            m_scrollRect.onValueChanged.AddListener((value) =>
            {
                if (UnityEngine.Input.GetMouseButton(0))
                {
                    m_forceScrollToTail = (value.y < 0.1f);
                }
            });

            StartCoroutine(CloneAdapter());
        }

        private void Update()
        {
            if (m_forceScrollToTail && !UnityEngine.Input.GetMouseButton(0))
            {
                m_scrollRect.verticalNormalizedPosition = 0.0f;
            }
        }
    }
}
