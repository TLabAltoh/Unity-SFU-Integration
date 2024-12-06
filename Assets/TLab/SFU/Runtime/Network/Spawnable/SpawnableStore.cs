using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    [CreateAssetMenu(fileName = "Spawnable Store", menuName = "TLab/SFU/Spawnable Store")]
    public class SpawnableStore : ScriptableObject
    {
        #region STRUCT


        [System.Serializable]
        public class StoreElement
        {
            public string name;
            public GameObject prefab;
            public GameObject distribute;
        }

        [System.Serializable]
        public struct StoreAction
        {
            [System.Serializable]
            public enum Action
            {
                None,
                Spawn,
                DeleteByUserId,
                DeleteByPublicId,
            }

            public static StoreAction GetSpawnAction(int elemId, int userId, Address32 publicId, WebTransform transform)
            {
                return new StoreAction(Action.Spawn, elemId, userId, publicId, transform);
            }

            public static StoreAction GetDeleteAction(int userId)
            {
                return new StoreAction(Action.DeleteByUserId, -1, userId, new Address32(), new WebTransform());
            }

            public static StoreAction GetDeleteAction(Address32 publicId)
            {
                return new StoreAction(Action.DeleteByPublicId, -1, -1, publicId, new WebTransform());
            }

            public StoreAction(Action action, int elemId, int userId, Address32 publicId, WebTransform transform)
            {
                this.action = action;
                this.elemId = elemId;
                this.userId = userId;
                this.publicId = publicId;
                this.transform = transform;
            }

            public Action action;
            public int elemId;
            public int userId;
            public Address32 publicId;
            public WebTransform transform;
        }

        public class History
        {
            public int userId;
            public NetworkObjectGroup group;
            public GameObject instance;

            public History(int userId, NetworkObjectGroup group, GameObject instance)
            {
                this.userId = userId;
                this.group = group;
                this.instance = instance;
            }
        }

        #endregion STRUCT

        #region REGISTORY

        private Hashtable m_registry = new Hashtable();

        private Hashtable m_map = new Hashtable();

        protected void Register(Address32 publicId, History history)
        {
            if (!m_registry.ContainsKey(publicId))
            {
                m_registry.Add(publicId, history);

                if (!m_map.ContainsKey(history.userId))
                    m_map[history.userId] = new List<Address32>();

                var map = m_map[history.userId] as List<Address32>;
                map.Add(publicId);
            }
        }

        protected void UnRegister(Address32 publicId)
        {
            if (m_registry.ContainsKey(publicId))
            {
                var history = m_registry[publicId] as History;
                m_registry.Remove(publicId);

                var map = m_map[history.userId] as List<Address32>;
                map.Remove(publicId);
            }
        }

        public void ClearRegistry()
        {
            var map = m_registry.Values.Cast<History>();

            foreach (var history in map)
                Destroy(history.instance);

            m_registry.Clear();
            m_map.Clear();
        }

        public History GetById(Address32 publicId) => m_registry[publicId] as History;

        #endregion REGISTORY

        [SerializeField] private List<StoreElement> m_store = new List<StoreElement>();

        private string THIS_NAME => "[" + this.GetType() + $"] ";

        public class Result
        {
            public StoreAction.Action action;
            public NetworkObjectGroup objectGroup;
            public GameObject instance;

            public Result(StoreAction.Action action, NetworkObjectGroup objectGroup, GameObject instance)
            {
                this.action = action;
                this.objectGroup = objectGroup;
                this.instance = instance;
            }

            public Result() { }
        }

        public void ProcessStoreAction(StoreAction storeAction, out Result result)
        {
            result = new Result();
            result.action = storeAction.action;

            switch (storeAction.action)
            {
                case StoreAction.Action.Spawn:
                    SpawnByElementId(storeAction.elemId, storeAction.userId, storeAction.publicId, storeAction.transform, out result.objectGroup, out result.instance);
                    return;
                case StoreAction.Action.DeleteByUserId:
                    DeleteByUserId(storeAction.userId);
                    return;
                case StoreAction.Action.DeleteByPublicId:
                    DeleteByPublicId(storeAction.publicId);
                    return;
            }
        }

        public bool DeleteByPublicId(Address32 publicId)
        {
            if (!m_registry.ContainsKey(publicId))
                return false;

            var history = m_registry[publicId] as History;
            Destroy(history.instance);

            m_registry.Remove(publicId);

            var map = m_map[history.userId] as List<Address32>;
            map.Remove(publicId);

            return true;
        }

        public bool DeleteByUserId(int userId)
        {
            if (!m_map.ContainsKey(userId))
                return false;

            var map = m_map[userId] as List<Address32>;
            map = new List<Address32>(map);
            map.ForEach((id) => DeleteByPublicId(id));

            return true;
        }

        public bool SpawnByElementId(int elemId, int userId, Address32 publicId, WebTransform @transform, out NetworkObjectGroup group, out GameObject instance)
        {
            if (!GetByElementId(elemId, userId, out var prefab))
            {
                Debug.LogWarning(THIS_NAME + "element is null !");
                instance = null;
                group = null;
                return false;
            }

            instance = Instantiate(prefab, @transform.position, @transform.rotation.ToQuaternion());

            group = instance.GetComponent<NetworkObjectGroup>();
            group.InitAllObjects(publicId, userId == NetworkClient.userId);

            Register(publicId, new History(userId, group, instance));

            return true;
        }

        public bool SpawnByElementName(string elemName, int userId, Address32 publicId, WebTransform @transform, out NetworkObjectGroup group, out GameObject instance)
        {
            GetByElementName(elemName, userId, out var prefab);

            instance = Instantiate(prefab, @transform.position, @transform.rotation.ToQuaternion());

            group = instance.GetComponent<NetworkObjectGroup>();
            group.InitAllObjects(publicId, userId == NetworkClient.userId);

            return true;
        }

        public bool GetByElementId(int elemId, int userId, out GameObject instance)
        {
            if (elemId > m_store.Count)
            {
                instance = null;
                return false;
            }

            if (NetworkClient.IsOwn(userId))
                instance = m_store[elemId].prefab;
            else
                instance = (m_store[elemId].distribute != null) ? m_store[(int)elemId].distribute : m_store[(int)elemId].prefab;

            return instance != null;
        }

        public bool GetByElementName(string elemName, int userId, out GameObject instance)
        {
            foreach (var elem in m_store)
            {
                if (elem.name == elemName)
                {
                    if (NetworkClient.IsOwn(userId))
                        instance = elem.prefab;
                    else
                        instance = (elem.distribute != null) ? elem.distribute : elem.prefab;

                    return true;
                }
            }

            instance = null;
            return false;
        }
    }
}
