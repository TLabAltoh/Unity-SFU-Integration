using System.Collections;
using System;
using System.Linq;
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

        private static MSG_SpawnableShop m_packet = new MSG_SpawnableShop();

        public virtual State GetLatestState() => new State(m_shopId, m_store.GetLatestActionArray());

        public virtual bool ProcessSpawnAction(SpawnableStore.SpawnAction spawnAction, out SpawnableStore.InstanceRef instanceRef)
        {
            var request = (spawnAction.action == SpawnableStore.SpawnAction.Action.RequestSpawn);

            if (request)
            {
                spawnAction.action = SpawnableStore.SpawnAction.Action.Spawn;
                if (UniqueNetworkId.GetAvailable(out var @public))
                    spawnAction.@public = @public;
                spawnAction.userId = NetworkClient.userId;
            }

            bool result = m_store.ProcessSpawnAction(spawnAction, out instanceRef);

            if (spawnAction.userId == NetworkClient.userId)
            {
                m_packet.action = spawnAction;
                NetworkClient.SendWS(m_packet.Marshall());
            }

            return result;
        }

        public virtual void Spawn(int elemId)
        {
            if (!m_anchor.Get(NetworkClient.userId, out var anchor))
                return;

            if (!UniqueNetworkId.GetAvailable(out var address))
                return;

            var action = SpawnableStore.SpawnAction.GetSpawnAction(elemId, address, anchor);

            ProcessSpawnAction(action, out var instanceRef);
        }

        public virtual void RequestSpawn(int elemId, int userId)
        {
            if (!m_anchor.Get(userId, out var anchor))
                return;

            var action = SpawnableStore.SpawnAction.GetRequestSpawnAction(elemId, userId, anchor);
            m_packet.action = action;
            NetworkClient.SendWS(userId, m_packet.Marshall());
        }

        protected virtual IEnumerator RequestSpawnForAllUserTask(int elemId)
        {
            var users = NetworkClient.GetLatestAvatorActionArray().Select((t) => t.userId);
            foreach (var user in users)
            {
                yield return new WaitForSeconds(0.25f);

                var skip = m_store.GetLatestActions().Any((t) => (user == t.userId) && (elemId == t.elemId));

                if (!skip)
                    RequestSpawn(elemId, user);
            }
        }

        public virtual void RequestSpawnForAllUserAsync(int elemId) => StartCoroutine(RequestSpawnForAllUserTask(elemId));

        protected virtual IEnumerator DeleteByElementIdTask(int elemId)
        {
            var targets = m_store.GetLatestActions().Where((t) => t.elemId == elemId);
            foreach (var target in targets)
            {
                yield return new WaitForSeconds(0.25f);
                var action = SpawnableStore.SpawnAction.GetDeleteAction(target.@public);
                m_packet.action = action;
                NetworkClient.SendWS(m_packet.Marshall());
            }
        }

        public virtual void DeleteByElementIdAsync(int elemId) => StartCoroutine(DeleteByElementIdTask(elemId));

        public virtual void SyncState(State state)
        {
            foreach (var action in state.latestActions)
                ProcessSpawnAction(action, out var instanceRef);
        }

        protected virtual void OnEnable()
        {
            Registry.Register(m_shopId, this);

            NetworkClient.RegisterOnMessage(m_packet.msgId, (from, to, bytes) =>
            {
                ProcessSpawnAction(m_packet.action, out var instanceRef);
            });

            NetworkClient.RegisterOnJoin(OnJoin, OnJoin);
            NetworkClient.RegisterOnExit(OnExit, OnExit);
        }

        protected virtual void OnDisable()
        {
            NetworkClient.UnRegisterOnJoin(OnJoin, OnJoin);
            NetworkClient.UnRegisterOnExit(OnExit, OnExit);
            NetworkClient.UnRegisterOnMessage(m_packet.msgId);

            Registry.UnRegister(m_shopId);
        }

        public virtual void OnJoin()
        {
            Debug.Log(THIS_NAME + $"{nameof(OnJoin)}");
        }

        public virtual void OnExit()
        {
            Debug.Log(THIS_NAME + $"{nameof(OnExit)}");
        }

        public virtual void OnJoin(int userId)
        {
            Debug.Log(THIS_NAME + $"{nameof(OnJoin)}");
        }

        public virtual void OnExit(int userId) => m_store?.DeleteByUserId(userId);
    }
}

