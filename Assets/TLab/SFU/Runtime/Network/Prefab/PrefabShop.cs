using UnityEngine;

namespace TLab.SFU.Network
{
    public class PrefabShop : MonoBehaviour, ISyncEventHandler
    {
        [SerializeField] private string m_storeName = "default";
        [SerializeField] private PrefabStore m_store;
        [SerializeField] private BaseAnchorProvider m_anchorProvider;

        private int m_pktId;

        public string storeName => m_storeName;

        public PrefabStore store => m_store;

        public int pktId => m_pktId;

        public bool GetAnchor(int id, out WebTransform anchor) => m_anchorProvider.Get(id, out anchor);

        private bool m_callOnce = true;

        private void OnEnable()
        {
            if (m_callOnce)
            {
                m_pktId = Packetable.MD5From(nameof(PrefabShop) + m_storeName);
                m_callOnce = false;
            }

            SyncClient.RegisterOnMessage(m_pktId, (from, to, bytes) =>
            {
                // TODO:

                // Instantiate by id

                // networked init

                // cache prefab to registory

                // boradcast
            });

            SyncClient.RegisterOnJoin(OnJoin, OnJoin);
            SyncClient.RegisterOnExit(OnExit, OnExit);
        }

        private void OnDisable()
        {
            SyncClient.UnRegisterOnJoin(OnJoin, OnJoin);
            SyncClient.UnRegisterOnExit(OnExit, OnExit);
            SyncClient.UnRegisterOnMessage(m_pktId);
        }

        public void OnJoin()
        {
            throw new System.NotImplementedException();
        }

        public void OnExit()
        {
            throw new System.NotImplementedException();
        }

        public void OnJoin(int userId)
        {
            throw new System.NotImplementedException();
        }

        public void OnExit(int userId) => m_store.DeleteByUserId(userId);
    }
}
