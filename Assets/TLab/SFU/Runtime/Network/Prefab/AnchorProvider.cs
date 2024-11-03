using UnityEngine;

namespace TLab.SFU.Network
{
    public abstract class BaseAnchorProvider : MonoBehaviour
    {
        public abstract bool Get(int id, out WebTransform anchor);
        public abstract bool Get(Address32 id, out WebTransform anchor);
        public abstract bool Get(Address64 id, out WebTransform anchor);
    }
}
