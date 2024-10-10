using System.Text;
using Unity.WebRTC;
using TLab.NetworkedVR.Network.WebRTC;

namespace TLab.NetworkedVR.Sample
{
    public class WebRTCClientSample : ClientSample
    {
        private WebRTCClient m_client;

        public override void Open()
        {
            m_client = WebRTCClient.Whep(this, m_adapter, STREAM, new RTCDataChannelInit(), false, false, OnMessage, OnDataChannelMessage);
        }

        public override void Close()
        {
            m_client?.HangUpDataChannel();
        }

        public override void Send(string message)
        {
            m_client.DataChannelSend(Encoding.UTF8.GetBytes(message));
        }

        public void OnDataChannelMessage(byte[] bytes)
        {
            OnMessage(this.gameObject.name + ": " + Encoding.UTF8.GetString(bytes));
        }
    }
}
