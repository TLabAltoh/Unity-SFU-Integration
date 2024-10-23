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

        protected UnityEvent<int, int, byte[]> m_onReceive;
        protected UnityEvent<int> m_onConnect;
        protected UnityEvent<int> m_onDisconnect;

        protected UnityAction<int, int, byte[]>[] m_events;

        public SfuClient(MonoBehaviour mono, Adapter adapter, string stream)
        {
            m_mono = mono;
            m_adapter = adapter;
            m_stream = stream;
        }

        public SfuClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<int, int, byte[]> onReceive, UnityEvent<int> onConnect, UnityEvent<int> onDisconnect) : this(mono, adapter, stream)
        {
            m_onReceive = onReceive;
            m_onConnect = onConnect;
            m_onDisconnect = onDisconnect;

            m_events = new UnityAction<int, int, byte[]>[] { OnReceive, OnConenct, OnDisconnect };
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

        private void OnReceive(int from, int to, byte[] bytes) => m_onReceive.Invoke(from, to, bytes);

        private void OnConenct(int from, int to, byte[] bytes) => m_onConnect.Invoke(from);

        private void OnDisconnect(int from, int to, byte[] bytes) => m_onDisconnect.Invoke(from);

        public virtual Task SendText(int to, string text) { return Task.Run(() => { }); }

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
