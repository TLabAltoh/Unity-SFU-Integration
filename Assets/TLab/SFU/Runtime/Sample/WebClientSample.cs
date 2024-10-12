using System.Text;
using Unity.WebRTC;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    public class WebClientSample : ClientSample
    {
        private SfuClient m_client;

        private bool m_useWebRTC = false;

        public override void Open()
        {
            m_client = m_useWebRTC ? WebRTCClient.Whep(this, m_adapter, STREAM, OnReceive, new RTCDataChannelInit(), false, false) : WebSocketClient.Open(this, m_adapter, STREAM, OnReceive);
        }

        public override void Close()
        {
            m_client?.HangUp();
        }

        public override void SendText(string message)
        {
            m_client.Send(Encoding.UTF8.GetBytes(message));
        }

        public override void Send(byte[] bytes)
        {
            m_client.Send(bytes);
        }

        public void OnReceive(byte[] bytes)
        {
            OnMessage(this.gameObject.name + ": " + Encoding.UTF8.GetString(bytes));
        }
    }
}
