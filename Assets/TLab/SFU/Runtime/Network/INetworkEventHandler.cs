namespace TLab.SFU.Network
{
    public interface INetworkEventHandler
    {
        void OnMessage(int from, int to, byte[] bytes);

        void OnOpen();

        void OnClose();

        void OnOpen(int from);

        void OnClose(int from);
    }
}
