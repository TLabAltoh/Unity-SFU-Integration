using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static System.BitConverter;
using static TLab.SFU.UnsafeUtility;

namespace TLab.SFU.Network
{
    public class Packetable
    {
        public const int HEADER_SIZE = 9;   // typ (1) + from (4) + to (4)

        public static int pktId;

        public static int MD5From(string @string)
        {
            // https://stackoverflow.com/a/26870764/22575350
            var hasher = MD5.Create();
            var hassed = hasher.ComputeHash(Encoding.UTF8.GetBytes(@string));
            return ToInt32(hassed);
        }

        static Packetable() => pktId = MD5From(nameof(Packetable));

        protected virtual int packetId => pktId;

        public Packetable() { }

        public Packetable(byte[] bytes) => UnMarshall(bytes);

        public virtual byte[] Marshall() => Combine(SfuClient.SEND_PACKET_HEADER_SIZE, packetId, Encoding.UTF8.GetBytes(JsonUtility.ToJson(this)));

        public virtual void UnMarshall(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes, NetworkClient.PAYLOAD_OFFSET, bytes.Length - NetworkClient.PAYLOAD_OFFSET);
            JsonUtility.FromJsonOverwrite(json, this);
        }
    }
}
