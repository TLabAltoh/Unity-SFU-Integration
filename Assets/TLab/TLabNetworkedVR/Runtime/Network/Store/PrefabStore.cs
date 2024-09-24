using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TLab.NetworkedVR.Network
{
    [CreateAssetMenu(fileName = "Prefab Store", menuName = "TLab/NetworkedVR/Prefab Store")]
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
        public class StoreAction
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
            public string publicId;
            public WebTransform transform;
        }

        #endregion STRUCT

        #region REGISTORY

        private Hashtable m_registry = new Hashtable();

        public Hashtable registry => m_registry;

        protected void Register(string publicId, GameObject instance)
        {
            if (!m_registry.ContainsKey(publicId))
            {
                m_registry[publicId] = instance;
            }
        }

        protected void UnRegister(string publicId)
        {
            if (m_registry.ContainsKey(publicId))
            {
                m_registry.Remove(publicId);
            }
        }

        public void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var controller = entry.Value as GameObject;
                gameobjects.Add(controller.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public GameObject GetByPublicId(int publicId)
        {
            return m_registry[publicId] as GameObject;
        }

        #endregion REGISTORY

        [SerializeField] private string m_storeName = "";

        [SerializeField] private List<StoreElement> m_store = new List<StoreElement>();

        public string storeName => m_storeName;

        public bool mchCallbackRegisted = false;

        public StoreAction.Action UpdateByInstantiateInfo(StoreAction prefabInstantiateInfo, out GameObject prefab)
        {
            switch (prefabInstantiateInfo.action)
            {
                case StoreAction.Action.INSTANTIATE:
                    {
                        InstantiateByElementId(prefabInstantiateInfo.elemId, prefabInstantiateInfo.userId, prefabInstantiateInfo.publicId, prefabInstantiateInfo.transform, out prefab);
                    }
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

        public StoreAction GenerateAction(StoreAction.Action action, int elemId, int userId, string publicId, WebTransform @transform)
        {
            return new StoreAction
            {
                action = action,
                elemId = elemId,
                publicId = publicId,
                transform = @transform,
            };
        }

        public bool RPCInstantiateByElementId(int elemId, int userId, string publicId, WebTransform @transform, out GameObject instance)
        {
            var result = InstantiateByElementId(elemId, userId, publicId, @transform, out instance);

            if (result)
            {
                // RPC
            }

            return result;
        }

        public bool RPCInstantiateByElementName(string elemName, int userId, string publicId, WebTransform @transform, out GameObject instance)
        {
            var result = InstantiateByElementName(elemName, userId, publicId, @transform, out instance);

            if (result)
            {
                // RPC
            }

            return result;
        }

        public bool InstantiateByElementId(int elemId, int userId, string publicId, WebTransform @transform, out GameObject instance)
        {
            GetByElementId(elemId, userId, out var prefab);

            instance = Instantiate(prefab, @transform.position.raw, @transform.rotation.rotation);

            instance.Foreach<NetworkedObject>((networkedObject) =>
            {
                networkedObject.Init(publicId);
            });

            Register(publicId, instance);

            return true;
        }

        public bool InstantiateByElementName(string elemName, int userId, string publicId, WebTransform @transform, out GameObject instance)
        {
            GetByElementName(elemName, userId, out var prefab);

            instance = Instantiate(prefab, @transform.position.raw, @transform.rotation.rotation);

            instance.Foreach<NetworkedObject>((networkedObject) =>
            {
                networkedObject.Init(publicId);
            });

            Register(publicId, instance);

            return true;
        }

        public bool GetByElementId(int elemId, int userId, out GameObject instance)
        {
            if (elemId > m_store.Count)
            {
                instance = null;
                return false;
            }

            if (SyncClient.IsOwn(userId))
            {
                instance = m_store[elemId].prefab;
            }
            else
            {
                instance = (m_store[elemId].distribute != null) ? m_store[(int)elemId].distribute : m_store[(int)elemId].prefab;
            }

            return true;
        }

        public bool GetByElementName(string elemName, int userId, out GameObject instance)
        {
            foreach (var elem in m_store)
            {
                if (elem.name == elemName)
                {
                    if (SyncClient.IsOwn(userId))
                    {
                        instance = elem.prefab;
                    }
                    else
                    {
                        instance = (elem.distribute != null) ? elem.distribute : elem.prefab;
                    }

                    return true;
                }
            }

            instance = null;
            return false;
        }

        public void Awake()
        {
            if (!mchCallbackRegisted)
            {
                SyncClient.RegisterMasterChannelCallback(m_storeName, (obj) =>
                {
                    // TODO:

                    // Instantiate by id

                    // networked init

                    // cache prefab to registory

                    // boradcast
                });

                mchCallbackRegisted = true;
            }
        }
    }
}
