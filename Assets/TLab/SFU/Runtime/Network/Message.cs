using System.Text;
using UnityEngine;
using static TLab.SFU.UnsafeUtility;

namespace TLab.SFU.Network
{
    public class Message
    {
        public Message() => Cache();

        public Message(byte[] bytes)
        {
            Cache();
            UnMarshall(bytes);
        }

        private void Cache()
        {
            m_attribute = (MessageAttribute)System.Attribute.GetCustomAttribute(GetType(), typeof(MessageAttribute), true);

            if (m_attribute != null)
                m_msgId = m_attribute.msgId;
        }

        private MessageAttribute m_attribute;
        private int m_msgId = 0;

        public int msgId => m_msgId;

        public static int GetMsgId<T>() where T : Message => Cryptography.MD5From(typeof(T).FullName);

        public static int GetMsgId<T>(string seed) where T : Message => Cryptography.MD5From(typeof(T).FullName + seed);

        public virtual unsafe byte[] Marshall()
        {
            var bytes = Padding(SfuClient.SEND_PACKET_HEADER_SIZE + sizeof(int), Encoding.UTF8.GetBytes(JsonUtility.ToJson(this)));
            Copy(msgId, bytes, SfuClient.SEND_PACKET_HEADER_SIZE);
            return bytes;
        }

        public virtual void UnMarshall(byte[] bytes) => JsonUtility.FromJsonOverwrite(Encoding.UTF8.GetString(bytes, NetworkClient.PAYLOAD_OFFSET, bytes.Length - NetworkClient.PAYLOAD_OFFSET), this);
    }
}
