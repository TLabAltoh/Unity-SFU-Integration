using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    using Registry = Registry<string, SpawnableShop>;

    public class SpawnableShop : MonoBehaviour, INetworkRoomEventHandler
    {
        [SerializeField] private const string m_shopId = "default";
        [SerializeField] private SpawnableStore m_store;
        [SerializeField] private BaseAnchorProvider m_anchor;

        private Dictionary<int, SpawnableStore.StoreAction> m_latestActions = new Dictionary<int, SpawnableStore.StoreAction>();
        public SpawnableStore.StoreAction[] latestActions => m_latestActions.Values.ToArray();

        [Serializable]
        public struct State
        {
            public string storeId;
            public SpawnableStore.StoreAction[] latestActions;

            public State(string storeId, SpawnableStore.StoreAction[] latestActions)
            {
                this.storeId = storeId;
                this.latestActions = latestActions;
            }
        }

        [Serializable, Message(typeof(MSG_SpawnableShop), m_shopId)]
        public class MSG_SpawnableShop : Message
        {
            public SpawnableStore.StoreAction action;
        }

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public string shopId => m_shopId;

        public SpawnableStore store => m_store;

        public BaseAnchorProvider anchor => m_anchor;

        private MSG_SpawnableShop m_tmp = new MSG_SpawnableShop();

        public State GetState() => new State(m_shopId, latestActions);

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

        private bool ProcessStoreAction(SpawnableStore.StoreAction avatorAction, out SpawnableStore.Result result)
        {
            result = new SpawnableStore.Result();

            switch (avatorAction.action)
            {
                case SpawnableStore.StoreAction.Action.Spawn:
                    if (!m_latestActions.ContainsKey(avatorAction.userId))
                    {
                        m_latestActions.Add(avatorAction.userId, avatorAction);

                        m_store.ProcessStoreAction(avatorAction, out result);

                        return true;
                    }
                    return false;
                case SpawnableStore.StoreAction.Action.DeleteByUserId:
                    if (m_latestActions.ContainsKey(avatorAction.userId))
                    {
                        m_latestActions.Remove(avatorAction.userId);

                        m_store.ProcessStoreAction(avatorAction, out result);

                        return true;
                    }
                    return false;
            }
            return false;
        }

        public void SyncState(State state)
        {
            foreach (var action in state.latestActions)
                ProcessStoreAction(action, out var result);
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
