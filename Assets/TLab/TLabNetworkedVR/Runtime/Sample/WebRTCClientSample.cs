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
            m_client = WebRTCClient.OpenChannel(this, m_adapter, STREAM, new RTCDataChannelInit(), OnResponse, OnDataChannelMessage);
        }

        public override void Close()
        {
            m_client?.HangUpDataChannel();
        }

        public void DataChannelSend(string message)
        {
            m_client.DataChannelSend(Encoding.UTF8.GetBytes(message));
        }

        public void OnDataChannelMessage(byte[] bytes)
        {
            OnMessage(this.gameObject.name + ": " + Encoding.UTF8.GetString(bytes));
        }
    }
}
