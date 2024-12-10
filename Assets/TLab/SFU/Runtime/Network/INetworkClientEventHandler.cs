namespace TLab.SFU.Network
{
    public interface INetworkClientEventHandler
    {
        void OnJoin();

        void OnExit();

        void OnJoin(int userId);

        void OnExit(int userId);
    }
}
