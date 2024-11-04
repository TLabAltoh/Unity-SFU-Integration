namespace TLab.SFU.Network
{
    public interface INetworkSyncEventHandler
    {
        void OnJoin();

        void OnExit();

        void OnJoin(int userId);

        void OnExit(int userId);
    }
}
