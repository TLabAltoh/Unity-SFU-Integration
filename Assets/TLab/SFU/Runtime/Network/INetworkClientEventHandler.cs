namespace TLab.SFU.Network
{
    /// <summary>
    /// Handler for NetworkClient's session event.
    /// </summary>
    public interface INetworkClientEventHandler
    {
        void OnJoin();

        void OnExit();

        void OnJoin(int userId);

        void OnExit(int userId);
    }
}
