using UnityEngine;

namespace TLab.SFU.Network
{
    public class PrefabShop : MonoBehaviour
    {
        [SerializeField] private string m_storeName = "default";
        [SerializeField] private PrefabStore m_store;
        [SerializeField] private Transform[] m_anchors;

        private int m_pktId;

        public string storeName => m_storeName;

        public PrefabStore store => m_store;

        public int pktId => m_pktId;

        [SerializeField, HideInInspector] private bool m_awaked = false;

        public WebTransform GetAnchor(int id)
        {
            if (id > m_anchors.Length)
                return new WebTransform(new WebVector3(), new WebVector4());

            var anchor = m_anchors[id];
            var @transform = new WebTransform(anchor.position, anchor.rotation);

            return @transform;
        }

        private void Awake()
        {
            if (!m_awaked)
            {
                m_pktId = Packetable.MD5From(m_storeName);

                SyncClient.RegisterOnMessage(m_pktId, (from, to, bytes) =>
                {
                    // TODO:

                    // Instantiate by id

                    // networked init

                    // cache prefab to registory

                    // boradcast
                });

                m_awaked = true;
            }
        }
    }
}
