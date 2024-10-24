namespace TLab.SFU.Network
{
    public interface ISyncEventHandler
    {
        void OnJoin();

        void OnExit();

        void OnJoin(int userId);

        void OnExit(int userId);
    }
}
