using System.Threading.Tasks;
using System.Collections;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;
using static System.BitConverter;

namespace TLab.SFU.Network
{
    public class WebSocketClient : SfuClient
    {
        private WebSocket m_socket;
        private Coroutine m_receiveTask;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        #region STRUCT

        [Serializable]
        public class StreamRequest : RequestAuth
        {
            public string stream;

            public StreamRequest(RequestAuth auth, string stream) : base(auth)
            {
                this.stream = stream;
            }

            [Serializable]
            public new class RustFormat
            {
                public int room_id;
                public int user_id;
                public uint token;
                public string stream;
                public string shared_key;

                public RustFormat(int roomId, string sharedKey, int userId, uint token, string stream)
                {
                    this.room_id = roomId;
                    this.user_id = userId;
                    this.token = token;
                    this.stream = stream;
                    this.shared_key = sharedKey;
                }
            }

            public override string ToJson() => JsonUtility.ToJson(new RustFormat(roomId, sharedKey, userId, token, stream));
        }

        #endregion STRUCT

        public WebSocketClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, (UnityEvent, UnityEvent<int>) onOpen, (UnityEvent, UnityEvent<int>) onClose, UnityEvent onError) : base(mono, adapter, stream, onMessage, onOpen, onClose, onError)
        {
            var base64 = "";

            base64 = Http.GetBase64(new StreamRequest(m_adapter.GetRequestAuth(), stream).ToJson());

            var url = "ws://" + m_adapter.config.GetHostPort() + $"/ws/connect/{base64}/";

            m_socket = new WebSocket(url);
            m_socket.OnOpen += () =>
            {
                Debug.Log(THIS_NAME + "Open !");
                MainThreadUtil.synchronizationContext.Post((__) =>
                {
                    RestartReceiveTask();
                    OnOpen1();
                }, null);
            };
            m_socket.OnError += (e) =>
            {
                Debug.LogError(THIS_NAME + "Error ! " + e);
                MainThreadUtil.synchronizationContext.Post((__) =>
                {
                    OnError();
                }, null);
            };
            m_socket.OnClose += (e) =>
            {
                Debug.Log(THIS_NAME + "Close !");
                MainThreadUtil.synchronizationContext.Post((__) =>
                {
                    StopCurrentReceiveTask();
                    OnClose1();
                }, null);
            };
            m_socket.OnMessage += OnPacket;
            _ = m_socket.Connect();
        }

        public WebSocketClient(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onMessage, (UnityAction, UnityAction<int>) onOpen, (UnityAction, UnityAction<int>) onClose, UnityAction onError)
            : this(mono, adapter, stream, CreateEvent(onMessage), (CreateEvent(onOpen.Item1), CreateEvent(onOpen.Item2)), (CreateEvent(onClose.Item1), CreateEvent(onClose.Item2)), CreateEvent(onError))
        {

        }

        public static WebSocketClient Open(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, (UnityEvent, UnityEvent<int>) onOpen, (UnityEvent, UnityEvent<int>) onClose, UnityEvent onError)
        {
            var client = new WebSocketClient(mono, adapter, stream, onMessage, onOpen, onClose, onError);

            return client;
        }

        public static WebSocketClient Open(MonoBehaviour mono, Adapter adapter, string stream, UnityAction<int, int, byte[]> onMessage, (UnityAction, UnityAction<int>) onOpen, (UnityAction, UnityAction<int>) onClose, UnityAction onError)
        {
            return Open(mono, adapter, stream, CreateEvent(onMessage), (CreateEvent(onOpen.Item1), CreateEvent(onOpen.Item2)), (CreateEvent(onClose.Item1), CreateEvent(onClose.Item2)), CreateEvent(onError));
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

        private void StopCurrentReceiveTask()
        {
            if ((m_receiveTask != null) && (m_mono != null))
                m_mono.StopCoroutine(m_receiveTask);

            m_receiveTask = null;
        }

        private void RestartReceiveTask()
        {
            StopCurrentReceiveTask();

            m_receiveTask = m_mono.StartCoroutine(ReceiveTask());
        }

        public bool connected
        {
            get
            {
                if (m_socket == null)
                    return false;

                return m_socket.State == WebSocketState.Open;
            }
        }

        public unsafe override Task Send(int to, byte[] bytes)
        {
            var headderBuf = GetBytes(to);
            fixed (byte* bytesPtr = bytes, headderBufPtr = headderBuf)
                UnsafeUtility.LongCopy(headderBufPtr, bytesPtr, sizeof(int));
            return m_socket.Send(bytes);
        }

        public override Task Send(int to, string text) => Send(to, Encoding.UTF8.GetBytes(text));

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
