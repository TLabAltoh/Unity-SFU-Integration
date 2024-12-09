using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    [CreateAssetMenu(fileName = "Spawnable Store", menuName = "TLab/SFU/Spawnable Store")]
    public class SpawnableStore : ScriptableObject
    {
        #region STRUCT


        [Serializable]
        public class StoreElement
        {
            public string name;
            public GameObject prefab;
            public GameObject distribute;
        }

        [Serializable]
        public struct SpawnAction
        {
            [Serializable]
            public enum Action
            {
                None,
                Spawn,
                DeleteByUserId,
                DeleteByPublicId,
            }

            public static SpawnAction GetSpawnAction(int elemId, int userId, Address32 publicId, WebTransform transform)
            {
                return new SpawnAction(Action.Spawn, elemId, userId, publicId, transform);
            }

            public static SpawnAction GetDeleteAction(int userId)
            {
                return new SpawnAction(Action.DeleteByUserId, -1, userId, new Address32(), new WebTransform());
            }

            public static SpawnAction GetDeleteAction(Address32 publicId)
            {
                return new SpawnAction(Action.DeleteByPublicId, -1, -1, publicId, new WebTransform());
            }

            public SpawnAction(Action action, int elemId, int userId, Address32 publicId, WebTransform transform)
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

        public class InstanceRef : ICloneable
        {
            public int userId;
            public SpawnAction.Action action;
            public SpawnAction spawnAction;
            public Address32 publicId;
            public GameObject instance;
            public NetworkObjectGroup objectGroup;

            public InstanceRef(int userId, SpawnAction.Action action, SpawnAction spawnAction, Address32 publicId, NetworkObjectGroup objectGroup, GameObject instance)
            {
                this.userId = userId;
                this.action = action;
                this.spawnAction = spawnAction;
                this.publicId = publicId;
                this.objectGroup = objectGroup;
                this.instance = instance;
            }

            public InstanceRef() { }

            public object Clone() => new InstanceRef(userId, action, spawnAction, publicId, objectGroup, instance);
        }

        #endregion STRUCT

        #region REGISTORY

        private Dictionary<Address32, InstanceRef> m_registry = new Dictionary<Address32, InstanceRef>();

        private Dictionary<int, List<Address32>> m_map = new Dictionary<int, List<Address32>>();

        public Dictionary<Address32, InstanceRef> registry => m_registry;

        protected void Register(Address32 publicId, InstanceRef instance)
        {
            if (!m_registry.ContainsKey(publicId))
            {
                m_registry.Add(publicId, instance);

                if (!m_map.ContainsKey(instance.userId))
                    m_map[instance.userId] = new List<Address32>();

                var map = m_map[instance.userId];
                map.Add(publicId);
            }
        }

        protected void UnRegister(Address32 publicId)
        {
            if (m_registry.ContainsKey(publicId))
            {
                var instance = m_registry[publicId];
                m_registry.Remove(publicId);

                var map = m_map[instance.userId];
                map.Remove(publicId);
            }
        }

        public void ClearRegistry()
        {
            var map = m_registry.Values.Cast<InstanceRef>();

            foreach (var instance in map)
                Destroy(instance.instance);

            m_registry.Clear();
            m_map.Clear();
        }

        public IEnumerable<SpawnAction> GetLatestActions() => m_registry.Values.Select((t) => t.spawnAction);

        public SpawnAction[] GetLatestActionArray() => GetLatestActions().ToArray();

        public IEnumerable<InstanceRef> GetByUserId(int userId) => m_registry.Where((t) => t.Value.userId == userId).Select((t) => t.Value);

        public InstanceRef GetByPublicId(Address32 publicId) => m_registry[publicId];

        #endregion REGISTORY

        [SerializeField] private List<StoreElement> m_store = new List<StoreElement>();

        private string THIS_NAME => "[" + this.GetType() + $"] ";

        public bool ProcessSpawnAction(SpawnAction spawnAction, out InstanceRef instanceRef)
        {
            instanceRef = new InstanceRef();
            instanceRef.action = spawnAction.action;

            switch (spawnAction.action)
            {
                case SpawnAction.Action.Spawn:
                    return SpawnByElementId(spawnAction.elemId, spawnAction.userId, spawnAction.publicId, spawnAction.transform, out instanceRef);
                case SpawnAction.Action.DeleteByUserId:
                    return DeleteByUserId(spawnAction.userId);
                case SpawnAction.Action.DeleteByPublicId:
                    return DeleteByPublicId(spawnAction.publicId);
            }

            return false;
        }

        public bool DeleteByPublicId(Address32 publicId)
        {
            if (!m_registry.ContainsKey(publicId))
                return false;

            var instance = m_registry[publicId];
            Destroy(instance.instance);

            m_registry.Remove(publicId);

            var map = m_map[instance.userId];
            map.Remove(publicId);

            return true;
        }

        public bool DeleteByUserId(int userId)
        {
            if (!m_map.ContainsKey(userId))
                return false;

            var map = m_map[userId];
            map = new List<Address32>(map);
            map.ForEach((id) => DeleteByPublicId(id));

            return true;
        }

        public bool SpawnByElementId(int elemId, int userId, Address32 publicId, WebTransform @transform, out InstanceRef instanceRef)
        {
            if (!GetByElementId(elemId, userId, out var prefab))
            {
                Debug.LogWarning(THIS_NAME + "element is null !");

                instanceRef = new InstanceRef();
                instanceRef.action = SpawnAction.Action.Spawn;

                return false;
            }

            var instance = Instantiate(prefab, @transform.position, @transform.rotation.ToQuaternion());

            var group = instance.GetComponent<NetworkObjectGroup>();
            var self = userId == NetworkClient.userId;
            group.InitAllObjects(publicId, self);

            var spawnAction = new SpawnAction(SpawnAction.Action.Spawn, elemId, userId, publicId, transform);
            instanceRef = new InstanceRef(userId, SpawnAction.Action.Spawn, spawnAction, publicId, group, instance);
            Register(publicId, instanceRef.Clone() as InstanceRef);

            return true;
        }

        public bool SpawnByElementName(string elemName, int userId, Address32 publicId, WebTransform @transform, out InstanceRef instanceRef)
        {
            GetByElementName(elemName, userId, out var elemId, out var prefab);

            var instance = Instantiate(prefab, @transform.position, @transform.rotation.ToQuaternion());

            var group = instance.GetComponent<NetworkObjectGroup>();
            var self = userId == NetworkClient.userId;
            group.InitAllObjects(publicId, self);

            var spawnAction = new SpawnAction(SpawnAction.Action.Spawn, elemId, userId, publicId, transform);
            instanceRef = new InstanceRef(userId, SpawnAction.Action.Spawn, spawnAction, publicId, group, instance);
            Register(publicId, instanceRef.Clone() as InstanceRef);

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
                instance = (m_store[elemId].distribute != null) ? m_store[elemId].distribute : m_store[elemId].prefab;

            return instance != null;
        }

        public bool GetByElementName(string elemName, int userId, out int elemId, out GameObject instance)
        {
            for (int i = 0; i < m_store.Count; i++)
            {
                var elem = m_store[i];

                if (elem.name == elemName)
                {
                    elemId = i;

                    if (NetworkClient.IsOwn(userId))
                        instance = elem.prefab;
                    else
                        instance = (elem.distribute != null) ? elem.distribute : elem.prefab;

                    return true;
                }
            }

            elemId = -1;
            instance = null;

            return false;
        }
    }
}
