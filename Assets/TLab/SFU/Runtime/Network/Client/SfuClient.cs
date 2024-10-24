using System.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU.Network
{
    public class SfuClient
    {
        protected MonoBehaviour m_mono;
        protected Adapter m_adapter;
        protected string m_stream;

        protected UnityEvent<int, int, byte[]> m_onMessage;
        protected UnityEvent<int> m_onOpen;
        protected UnityEvent<int> m_onClose;

        protected UnityAction<int, int, byte[]>[] m_events;

        public SfuClient(MonoBehaviour mono, Adapter adapter, string stream)
        {
            m_mono = mono;
            m_adapter = adapter;
            m_stream = stream;
        }

        public SfuClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onMessage, UnityEvent<int> onOpen, UnityEvent<int> onClose) : this(mono, adapter, stream)
        {
            m_onMessage = onMessage;
            m_onOpen = onOpen;
            m_onClose = onClose;

            m_events = new UnityAction<int, int, byte[]>[] { OnMessage, OnOpen, OnClose };
        }

        public const int PACKET_HEADER_SIZE = 9;

        protected void OnPacket(byte[] bytes)
        {
            // typ (1) + from (4) + to (4) = 9

            var typ = bytes[0];
            var from = BitConverter.ToInt32(bytes, 1);
            var to = BitConverter.ToInt32(bytes, 1 + sizeof(int));

            m_events[typ].Invoke(from, to, bytes);
        }

        private void OnMessage(int from, int to, byte[] bytes) => m_onMessage.Invoke(from, to, bytes);

        private void OnOpen(int from, int to, byte[] bytes) => m_onOpen.Invoke(from);

        private void OnClose(int from, int to, byte[] bytes) => m_onClose.Invoke(from);

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
