namespace TLab.SFU.Network
{
    /// <summary>
    /// Handler for SfuClient's session event.
    /// </summary>
    public interface ISfuClientEventHandler
    {
        void OnOpen();

        void OnClose();

        void OnError();

        void OnOpen(int from);

        void OnClose(int from);

        void OnMessage(int from, int to, byte[] bytes);
    }
}
