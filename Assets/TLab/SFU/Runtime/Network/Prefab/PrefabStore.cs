using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    [CreateAssetMenu(fileName = "Prefab Store", menuName = "TLab/SFU/Prefab Store")]
    public class PrefabStore : ScriptableObject
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
                INSTANTIATE,
                DELETE,
                NONE
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
            public GameObject instance;

            public History(int userId, GameObject instance)
            {
                this.userId = userId;
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

        public StoreAction.Action UpdateByInstantiateInfo(StoreAction action, out GameObject prefab)
        {
            switch (action.action)
            {
                case StoreAction.Action.INSTANTIATE:
                    InstantiateByElementId(action.elemId, action.userId, action.publicId, action.transform, out prefab);
                    return StoreAction.Action.INSTANTIATE;
                case StoreAction.Action.DELETE:
                    {
                        prefab = null;
                        // TODO: DELETE PREFAB
                    }
                    return StoreAction.Action.DELETE;
            }

            prefab = null;
            return StoreAction.Action.NONE;
        }

        public StoreAction GenerateAction(StoreAction.Action action, int elemId, int userId, Address32 publicId, WebTransform @transform)
        {
            return new StoreAction
            {
                action = action,
                elemId = elemId,
                publicId = publicId,
                transform = @transform,
            };
        }

        public bool RPCInstantiateByElementId(int elemId, int userId, Address32 publicId, WebTransform @transform, out GameObject instance)
        {
            var result = InstantiateByElementId(elemId, userId, publicId, @transform, out instance);

            if (result)
            {
                // RPC
            }

            return result;
        }

        public bool RPCInstantiateByElementName(string elemName, int userId, Address32 publicId, WebTransform @transform, out GameObject instance)
        {
            var result = InstantiateByElementName(elemName, userId, publicId, @transform, out instance);

            if (result)
            {
                // RPC
            }

            return result;
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

        public bool InstantiateByElementId(int elemId, int userId, Address32 publicId, WebTransform @transform, out GameObject instance)
        {
            if (!GetByElementId(elemId, userId, out var prefab))
            {
                Debug.LogWarning(THIS_NAME + "element is null !");
                instance = null;
                return false;
            }

            instance = Instantiate(prefab, @transform.position, @transform.rotation.ToQuaternion());

            instance.Foreach<NetworkObject>((t) => t.Init(publicId));

            Register(publicId, new History(userId, instance));

            return true;
        }

        public bool InstantiateByElementName(string elemName, int userId, Address32 publicId, WebTransform @transform, out GameObject instance)
        {
            GetByElementName(elemName, userId, out var prefab);

            instance = Instantiate(prefab, @transform.position, @transform.rotation.ToQuaternion());

            instance.Foreach<NetworkObject>((t) => t.Init(publicId));

            Register(publicId, new History(userId, instance));

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
