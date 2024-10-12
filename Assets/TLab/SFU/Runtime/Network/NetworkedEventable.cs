namespace TLab.SFU.Network
{
    public interface NetworkedEventable
    {
        public void OnOthersJoined(int userId);

        public void OnOthersExited(int userId);
    }
}
