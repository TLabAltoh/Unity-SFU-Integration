using System;
using UnityEngine;

namespace TLab.SFU.Network
{
    using Registry = Registry<string, SpawnableShop>;

    public class SpawnableShop : MonoBehaviour, INetworkClientEventHandler
    {
        [SerializeField] private const string m_shopId = "default";
        [SerializeField] private SpawnableStore m_store;
        [SerializeField] private BaseAnchorProvider m_anchor;

        [Serializable]
        public struct State
        {
            public string storeId;
            public SpawnableStore.SpawnAction[] latestActions;

            public State(string storeId, SpawnableStore.SpawnAction[] latestActions)
            {
                this.storeId = storeId;
                this.latestActions = latestActions;
            }
        }

        [Serializable, Message(typeof(MSG_SpawnableShop), m_shopId)]
        public class MSG_SpawnableShop : Message
        {
            public SpawnableStore.SpawnAction action;
        }

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public string shopId => m_shopId;

        public SpawnableStore store => m_store;

        public BaseAnchorProvider anchor => m_anchor;

        private MSG_SpawnableShop m_tmp = new MSG_SpawnableShop();

        public State GetState() => new State(m_shopId, m_store.GetLatestActionArray());

        //public bool RPCInstantiateByElementId(int elemId, int userId, Address32 publicId, WebTransform @transform, out GameObject instance)
        //{
        //    var result = InstantiateByElementId(elemId, userId, publicId, @transform, out instance);

        //    if (result)
        //    {
        //        // RPC
        //    }

        //    return result;
        //}

        //public bool RPCInstantiateByElementName(string elemName, int userId, Address32 publicId, WebTransform @transform, out GameObject instance)
        //{
        //    var result = InstantiateByElementName(elemName, userId, publicId, @transform, out instance);

        //    if (result)
        //    {
        //        // RPC
        //    }

        //    return result;
        //}

        private bool ProcessSpawnAction(SpawnableStore.SpawnAction spawnAction, out SpawnableStore.InstanceRef instanceRef) => m_store.ProcessSpawnAction(spawnAction, out instanceRef);

        public void SyncState(State state)
        {
            foreach (var action in state.latestActions)
                ProcessSpawnAction(action, out var instanceRef);
        }

        private void OnEnable()
        {
            Registry.Register(m_shopId, this);

            NetworkClient.RegisterOnMessage(m_tmp.msgId, (from, to, bytes) =>
            {
                // TODO:

                // Spawn by elementId

                // NetworkObject.Init()

                // Cache instance to registory

                // Boradcast
            });

            NetworkClient.RegisterOnJoin(OnJoin, OnJoin);
            NetworkClient.RegisterOnExit(OnExit, OnExit);
        }

        private void OnDisable()
        {
            NetworkClient.UnRegisterOnJoin(OnJoin, OnJoin);
            NetworkClient.UnRegisterOnExit(OnExit, OnExit);
            NetworkClient.UnRegisterOnMessage(m_tmp.msgId);

            Registry.UnRegister(m_shopId);
        }

        public void OnJoin()
        {
            Debug.Log(THIS_NAME + $"{nameof(OnJoin)}");
        }

        public void OnExit()
        {
            Debug.Log(THIS_NAME + $"{nameof(OnExit)}");
        }

        public void OnJoin(int userId)
        {
            Debug.Log(THIS_NAME + $"{nameof(OnJoin)}");
        }

        public void OnExit(int userId) => m_store?.DeleteByUserId(userId);
    }
}
