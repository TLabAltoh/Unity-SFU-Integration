using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;

namespace TLab.SFU.Network
{
    public class WebSocketClient : SfuClient
    {
        private WebSocket m_socket;
        private Coroutine m_receiveTask;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        #region STRUCT

        [System.Serializable]
        public class StreamRequest : RequestAuth
        {
            public string stream;

            public StreamRequest(RequestAuth auth, string stream) : base(auth)
            {
                this.stream = stream;
            }
        }

        #endregion STRUCT

        public WebSocketClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<byte[]> onReceive) : base(mono, adapter, stream, onReceive)
        {
            var base64 = "";

            base64 = Http.GetBase64(new StreamRequest(m_adapter.GetRequestAuth(), stream));

            var url = "ws://" + m_adapter.room.config.GetHostPort() + $"/ws/connect/{base64}/";

            m_socket = new WebSocket(url);
            m_socket.OnOpen += () =>
            {
                Debug.Log(THIS_NAME + "Connection open!");
                CancelReceiveTask();
                m_receiveTask = m_mono.StartCoroutine(ReceiveTask());
            };
            m_socket.OnError += (e) => Debug.Log(THIS_NAME + "Error! " + e);
            m_socket.OnClose += (e) => Debug.Log(THIS_NAME + "Connection closed!");
            m_socket.OnMessage += (bytes) => m_onReceive.Invoke(bytes);
            _ = m_socket.Connect();
        }

        public WebSocketClient(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<byte[]> onReceive)
            : this(mono, adapter, stream, CreateOnReceive(onReceive))
        {

        }

        public static WebSocketClient Open(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<byte[]> onReceive)
        {
            var client = new WebSocketClient(mono, adapter, stream, onReceive);

            return client;
        }

        public static WebSocketClient Open(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<byte[]> onReceive)
        {
            return Open(mono, adapter, stream, CreateOnReceive(onReceive));
        }

        private void CancelReceiveTask()
        {
            if (m_receiveTask != null)
            {
                m_mono.StopCoroutine(m_receiveTask);
            }
            m_receiveTask = null;
        }

        private IEnumerator ReceiveTask()
        {
            while (true)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                m_socket?.DispatchMessageQueue();
#endif
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
