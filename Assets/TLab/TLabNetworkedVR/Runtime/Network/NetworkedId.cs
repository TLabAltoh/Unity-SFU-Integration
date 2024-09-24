using UnityEngine;

namespace TLab.NetworkedVR.Network
{
    public class NetworkedId : MonoBehaviour
    {
        protected string m_id = "";

        protected string m_publicId = "";

        public string id => m_publicId + m_id;

        public string publicId => m_publicId;

        public virtual void SetPublicId(string id)
        {
            m_id = id;
        }

#if UNITY_EDITOR
        public virtual void CreateHashID()
        {
            var r = new System.Random();
            var v = r.Next();

            m_id = v.GetHashCode().ToString();
        }
#endif
    }
}
