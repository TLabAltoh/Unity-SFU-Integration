using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU.Network
{
    public class SfuClient
    {
        protected MonoBehaviour m_mono;
        protected Adapter m_adapter;
        protected string m_stream;
        protected UnityEvent<byte[]> m_onReceive;

        public SfuClient(MonoBehaviour mono, Adapter adapter, string stream)
        {
            m_mono = mono;
            m_adapter = adapter;
            m_stream = stream;
        }

        public SfuClient(MonoBehaviour mono, Adapter adapter, string stream, UnityEvent<byte[]> onReceive) : this(mono, adapter, stream)
        {
            m_onReceive = onReceive;
        }

        public virtual void SendText(string text) { }

        public virtual void Send(byte[] bytes) { }

        public virtual void HangUp() { }

        public static UnityEvent<byte[]> CreateOnReceive(params UnityAction<byte[]>[] @actions)
        {
            var @event = new UnityEvent<byte[]>();
            foreach (var @action in @actions)
            {
                @event.AddListener(@action);
            }

            return @event;
        }
    }
}
