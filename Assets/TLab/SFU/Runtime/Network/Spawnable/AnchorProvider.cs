using UnityEngine;

namespace TLab.SFU.Network
{
    public abstract class BaseAnchorProvider : MonoBehaviour
    {
        public abstract bool Get(int id, out SerializableTransform anchor);
        public abstract bool Get(in Address32 id, out SerializableTransform anchor);
        public abstract bool Get(in Address64 id, out SerializableTransform anchor);
    }
}
