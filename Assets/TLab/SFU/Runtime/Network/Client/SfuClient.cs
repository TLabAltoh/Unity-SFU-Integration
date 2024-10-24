using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static System.BitConverter;

namespace TLab.SFU.Network
{
    public class SfuClient
    {
        protected MonoBehaviour m_mono;
        protected Adapter m_adapter;
        protected string m_stream;

        protected UnityEvent<int, int, byte[]> m_onMessage;
        protected (UnityEvent, UnityEvent<int>) m_onOpen;
        protected (UnityEvent, UnityEvent<int>) m_onClose;
        protected UnityEvent m_onError;

        protected UnityAction<int, int, byte[]>[] m_onPacket;

        public SfuClient(MonoBehaviour mono, Adapter adapter, string stream)
        {
            m_mono = mono;
            m_adapter = adapter;
            m_stream = stream;
        }

        public SfuClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, (UnityEvent, UnityEvent<int>) onOpen, (UnityEvent, UnityEvent<int>) onClose, UnityEvent onError) : this(mono, adapter, stream)
        {
            m_onMessage = onMessage;
            m_onOpen = onOpen;
            m_onClose = onClose;
            m_onError = onError;

            m_onPacket = new UnityAction<int, int, byte[]>[] { OnMessage, OnOpen2, OnClose2 };
        }

        public const int PACKET_HEADER_SIZE = 9;

        protected void OnPacket(byte[] bytes)
        {
            // typ (1) + from (4) + to (4) = 9

            var typ = bytes[0];
            var from = ToInt32(bytes, 1);
            var to = ToInt32(bytes, 1 + sizeof(int));

            m_onPacket[typ].Invoke(from, to, bytes);
        }

        protected void OnMessage(int from, int to, byte[] bytes) => m_onMessage.Invoke(from, to, bytes);

        protected void OnError() => m_onError.Invoke();

        protected void OnOpen1() => m_onOpen.Item1.Invoke();

        protected void OnClose1() => m_onClose.Item1.Invoke();

        protected void OnOpen2(int from, int to, byte[] bytes) => m_onOpen.Item2.Invoke(from);

        protected void OnClose2(int from, int to, byte[] bytes) => m_onClose.Item2.Invoke(from);

        public virtual Task Send(int to, string text) { return Task.Run(() => { }); }

        public virtual Task Send(int to, byte[] bytes) { return Task.Run(() => { }); }

        public virtual Task HangUp() { return Task.Run(() => { }); }

        public static UnityEvent<T> CreateEvent<T>(params UnityAction<T>[] @actions)
        {
            var @event = new UnityEvent<T>();
            foreach (var @action in @actions)
                @event.AddListener(@action);
            return @event;
        }

        public static UnityEvent CreateEvent(params UnityAction[] @actions)
        {
            var @event = new UnityEvent();
            foreach (var @action in @actions)
                @event.AddListener(@action);
            return @event;
        }

        public static UnityEvent<T0, T1> CreateEvent<T0, T1>(params UnityAction<T0, T1>[] @actions)
        {
            var @event = new UnityEvent<T0, T1>();
            foreach (var @action in @actions)
                @event.AddListener(@action);
            return @event;
        }

        public static UnityEvent<T0, T1, T2> CreateEvent<T0, T1, T2>(params UnityAction<T0, T1, T2>[] @actions)
        {
            var @event = new UnityEvent<T0, T1, T2>();
            foreach (var @action in @actions)
                @event.AddListener(@action);
            return @event;
        }
    }
}
