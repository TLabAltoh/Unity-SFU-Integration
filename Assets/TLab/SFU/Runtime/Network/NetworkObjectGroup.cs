using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Network Object Group (TLab)")]
    public class NetworkObjectGroup : MonoBehaviour
    {
        [SerializeField] private NetworkObject[] m_registry;

        private NetworkObject.State m_state = NetworkObject.State.None;

        public NetworkObject.State state => m_state;

        public int length
        {
            get
            {
                if (m_registry == null)
                    return 0;
                return m_registry.Length;
            }
        }

        public void UpdateRegistry()
        {
            var tmp0 = GetComponentsInChildren<NetworkObject>();
            var tmp1 = new List<NetworkObject>();

            foreach (var @object in tmp0)
            {
                var group = GetComponentInParent<NetworkObjectGroup>(true);
                if (group == this)
                    tmp1.Add(@object);
            }

            m_registry = tmp1.ToArray();
        }

        public void InitAllObjects(bool self)
        {
            if (!self)
                StartCoroutine(WaitForInitialized());

            m_registry.Foreach((t) => t.Init(self));
        }

        public void InitAllObjects(Address32 publicId, bool self)
        {
            if (!self)
                StartCoroutine(WaitForInitialized());

            m_registry.Foreach((t) => t.Init(publicId, self));
        }

        private IEnumerator WaitForInitialized()
        {
            m_state = NetworkObject.State.Waiting0;

            var complete = false;
            while (!complete)
            {
                m_registry.Foreach((t) => complete &= ((t.state == NetworkObject.State.Waiting1) || (t.state == NetworkObject.State.Initialized)));
                yield return new WaitForSeconds(0.1f);
            }
            m_registry.Foreach((t) => t.NoticeInitComplete());

            m_state = NetworkObject.State.Initialized;
        }
    }
}
