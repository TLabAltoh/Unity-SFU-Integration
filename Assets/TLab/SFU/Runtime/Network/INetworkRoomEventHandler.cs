namespace TLab.SFU.Network
{
    public interface INetworkRoomEventHandler
    {
        void OnJoin();

        void OnExit();

        void OnJoin(int userId);

        void OnExit(int userId);
    }
}
