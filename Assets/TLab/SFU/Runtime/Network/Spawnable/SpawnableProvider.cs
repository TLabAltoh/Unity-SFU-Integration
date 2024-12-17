using System.Collections;
using System;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    public class SpawnableProvider : MonoBehaviour, INetworkClientEventHandler
    {
        [SerializeField] private SpawnableStore m_store;
        [SerializeField] private BaseAnchorProvider m_anchor;

        [Serializable, Message(typeof(MSG_SpawnableProvider))]
        public class MSG_SpawnableProvider : Message
        {
            public SpawnableStore.SpawnAction action;
        }

        private static SpawnableProvider m_instance;
        public static SpawnableProvider instance => m_instance;

        private static string THIS_NAME => "[" + typeof(SpawnableProvider).FullName + "] ";

        private static MSG_SpawnableProvider m_packet = new MSG_SpawnableProvider();

        public static SpawnableStore.SpawnAction[] GetLatestActionArray() => instance.m_store.GetLatestActionArray();

        public static bool ProcessSpawnAction(SpawnableStore.SpawnAction spawnAction, out SpawnableStore.InstanceRef instanceRef)
        {
            var request = (spawnAction.action == SpawnableStore.SpawnAction.Action.RequestSpawn);

            if (request)
            {
                spawnAction.action = SpawnableStore.SpawnAction.Action.Spawn;
                if (UniqueNetworkId.GetAvailable(out var @public))
                    spawnAction.@public = @public;
                spawnAction.userId = NetworkClient.userId;
            }

            bool result = instance.m_store.ProcessSpawnAction(spawnAction, out instanceRef);

            if (spawnAction.userId == NetworkClient.userId)
            {
                m_packet.action = spawnAction;
                NetworkClient.SendWS(m_packet.Marshall());
            }

            return result;
        }

        public static void Spawn(int elemId)
        {
            if (!instance.m_anchor.Get(NetworkClient.userId, out var anchor))
                return;

            if (!UniqueNetworkId.GetAvailable(out var address))
                return;

            var action = SpawnableStore.SpawnAction.GetSpawnAction(elemId, address, anchor);

            ProcessSpawnAction(action, out var instanceRef);
        }

        public static void RequestSpawn(int elemId, int userId)
        {
            if (!instance.m_anchor.Get(userId, out var anchor))
                return;

            if (userId == NetworkClient.userId)
            {
                var action = SpawnableStore.SpawnAction.GetRequestSpawnAction(elemId, userId, anchor);
                ProcessSpawnAction(action, out var instanceRef);
            }
            else
            {
                var action = SpawnableStore.SpawnAction.GetRequestSpawnAction(elemId, userId, anchor);
                m_packet.action = action;
                NetworkClient.SendWS(userId, m_packet.Marshall());
            }

            Debug.Log(THIS_NAME + $"{nameof(RequestSpawn)}:{elemId}:{userId}");
        }

        protected static IEnumerator RequestSpawnForAllUserTask(int elemId)
        {
            var users = NetworkClient.GetLatestAvatorActionArray().Select((t) => t.userId);
            foreach (var user in users)
            {
                if (user is not 0)
                    continue;

                yield return new WaitForSeconds(0.25f);

                var skip = instance.m_store.GetLatestActions().Any((t) => (user == t.userId) && (elemId == t.elemId));

                if (!skip)
                    RequestSpawn(elemId, user);
            }
        }

        public static void RequestSpawnForAllUserAsync(int elemId) => instance.StartCoroutine(RequestSpawnForAllUserTask(elemId));

        protected static IEnumerator DeleteByElementIdTask(int elemId)
        {
            var targets = instance.m_store.GetLatestActions().Where((t) => t.elemId == elemId).ToArray();
            foreach (var target in targets)
            {
                yield return new WaitForSeconds(0.25f);

                var action = SpawnableStore.SpawnAction.GetDeleteAction(target.@public);
                ProcessSpawnAction(action, out var instanceRef);
            }
        }

        public static void DeleteByElementIdAsync(int elemId) => instance.StartCoroutine(DeleteByElementIdTask(elemId));

        public static void SyncLatestActions(SpawnableStore.SpawnAction[] latestActions)
        {
            foreach (var action in latestActions)
                ProcessSpawnAction(action, out var instanceRef);
        }

        protected virtual void OnEnable()
        {
            NetworkClient.RegisterOnMessage(m_packet.msgId, (from, to, bytes) =>
            {
                m_packet.UnMarshall(bytes);
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
        }

        protected virtual void Awake() => m_instance = this;

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

