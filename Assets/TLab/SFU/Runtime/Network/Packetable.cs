using System.Text;
using UnityEngine;
using static TLab.SFU.UnsafeUtility;

namespace TLab.SFU.Network
{
    public class Packetable
    {
        public const int HEADER_SIZE = 9;   // typ (1) + from (4) + to (4)

        public static int pktId;

        static Packetable() => pktId = Cryptography.MD5From(nameof(Packetable));

        protected virtual int packetId => pktId;

        public Packetable() { }

        public Packetable(byte[] bytes) => UnMarshall(bytes);

        public virtual byte[] Marshall() => Combine(SfuClient.SEND_PACKET_HEADER_SIZE, packetId, Encoding.UTF8.GetBytes(JsonUtility.ToJson(this)));

        public virtual void UnMarshall(byte[] bytes) => JsonUtility.FromJsonOverwrite(Encoding.UTF8.GetString(bytes, NetworkClient.PAYLOAD_OFFSET, bytes.Length - NetworkClient.PAYLOAD_OFFSET), this);
    }
}
