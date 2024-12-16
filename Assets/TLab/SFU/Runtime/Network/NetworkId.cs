using UnityEngine;

namespace TLab.SFU.Network
{
    public class NetworkId : MonoBehaviour
    {
        [SerializeField, HideInInspector] protected Address32 m_private;

        [SerializeField, HideInInspector] protected Address32 m_public;

        [SerializeField, HideInInspector] protected Address64 m_id;

        public Address32 @private => m_private;

        public Address32 @public => m_public;

        public Address64 id => m_id;

        public virtual void SetPublic(Address32 @public)
        {
            m_public.Copy(@public);
            m_id.CopyUpper32(@public);
        }

#if UNITY_EDITOR
        public virtual void SetPrivate(Address32 @private)
        {
            m_private.Copy(@private);
            m_id.CopyLower32(@private);

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
