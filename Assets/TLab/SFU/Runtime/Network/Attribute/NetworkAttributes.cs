using System;

namespace TLab.SFU.Network
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class MessageAttribute : Attribute
    {
        public int msgId;

        public Type type;

        public MessageAttribute(Type type)
        {
            this.type = type;

            msgId = Cryptography.MD5From(type.FullName);
        }

        public MessageAttribute(Type type, string seed)
        {
            this.type = type;

            msgId = Cryptography.MD5From(type.FullName + seed);
        }
    }
}
