using UnityEngine;

namespace TLab.SFU.Network
{
    public class NetworkId : MonoBehaviour
    {
        [SerializeField, HideInInspector] protected Address32 m_privateId;

        [SerializeField, HideInInspector] protected Address32 m_publicId;

        [SerializeField, HideInInspector] protected Address64 m_id;

        public Address32 privateId => m_privateId;

        public Address32 publicId => m_publicId;

        public Address64 id => m_id;

        public virtual void SetPublicId(Address32 publicId)
        {
            m_publicId.Copy(publicId);
            m_id.CopyUpper32(publicId);
        }

#if UNITY_EDITOR
        public virtual void GenerateAddress()
        {
            var r = new System.Random();
            var v = new byte[4];

            r.NextBytes(v);
            m_privateId.Update(v[0], v[1], v[2], v[3]);
            m_id.UpdateLower32(v[0], v[1], v[2], v[3]);

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
