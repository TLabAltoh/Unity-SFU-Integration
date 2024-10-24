using System.Threading.Tasks;
using System.Collections;
using System;
using System.Text;
using System.Linq;
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

        public WebSocketClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, UnityEvent<int> onOpen, UnityEvent<int> onClose) : base(mono, adapter, stream, onMessage, onOpen, onClose)
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
            m_socket.OnMessage += (bytes) => OnPacket(bytes);
            _ = m_socket.Connect();
        }

        public WebSocketClient(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onMessage, UnityAction<int> onOpen, UnityAction<int> onClose)
            : this(mono, adapter, stream, CreateEvent(onMessage), CreateEvent(onOpen), CreateEvent(onClose))
        {

        }

        public static WebSocketClient Open(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, UnityEvent<int> onOpen, UnityEvent<int> onClose)
        {
            var client = new WebSocketClient(mono, adapter, stream, onMessage, onOpen, onClose);

            return client;
        }

        public static WebSocketClient Open(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onMessage, UnityAction<int> onOpen, UnityAction<int> onClose)
        {
            return Open(mono, adapter, stream, CreateEvent(onMessage), CreateEvent(onOpen), CreateEvent(onClose));
        }

        private void CancelReceiveTask()
        {
            if (m_receiveTask != null)
                m_mono.StopCoroutine(m_receiveTask);

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

        public bool connected
        {
            get
            {
                if (m_socket == null)
                {
                    return false;
                }
                return m_socket.State == WebSocketState.Open;
            }
        }

        public override Task Send(int to, byte[] bytes)
        {
            var hedder = BitConverter.GetBytes(to);
            var packet = hedder.Concat(bytes);
            return m_socket.Send(packet.ToArray());
        }

        public override Task Send(int to, string text)
        {
            return Send(to, Encoding.UTF8.GetBytes(text));
        }

        public override Task HangUp()
        {
            if (m_socket == null)
            {
                Debug.Log(THIS_NAME + "Socket is already null");
                return base.HangUp();
            }

            return m_socket.Close();
        }
    }
}
