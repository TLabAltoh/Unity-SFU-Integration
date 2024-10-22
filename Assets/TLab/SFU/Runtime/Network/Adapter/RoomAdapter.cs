using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU.Network
{
    [CreateAssetMenu(fileName = "Room Adapter", menuName = "TLab/SFU/Room Adapter")]
    public class RoomAdapter : ScriptableObject
    {
        [SerializeField] private RoomConfig m_config;

        [SerializeField] private PrefabStore m_avatorStore;

        private Dictionary<int, PrefabStore.StoreAction> m_avatorInstantiateHistory = new Dictionary<int, PrefabStore.StoreAction>();

        private int m_id;

        public int id => m_id;

        public string password
        {
            get
            {
                m_config.GetAuth(out var room_pass, out var master_key);
                return room_pass;
            }
        }

        public RoomConfig config => m_config;

        public PrefabStore avatorStore => m_avatorStore;

        public PrefabStore.StoreAction[] avatorInstantiateHistorys
        {
            get
            {
                return m_avatorInstantiateHistory.Values.ToArray();
            }
        }

        public RoomAdapter(RoomConfig config)
        {
            m_config = config;
        }

        public void Init(RoomConfig config, int id)
        {
            m_config = config;

            m_id = id;
        }

        public RoomAdapter GetClone()
        {
            var instance = CreateInstance<RoomAdapter>();

            instance.Init(m_config, m_id);

            return instance;
        }

        public bool IsPlayerJoined(int index)
        {
            return m_avatorInstantiateHistory.ContainsKey(index);
        }

        public bool GetInstantiateInfo(int index, out PrefabStore.StoreAction info)
        {
            if (m_avatorInstantiateHistory.ContainsKey(index))
            {
                info = m_avatorInstantiateHistory[index];
                return true;
            }
            else
            {
                info = new PrefabStore.StoreAction();
                return false;
            }
        }

        public bool UpdateState(PrefabStore.StoreAction info, out GameObject avator)
        {
            avator = null;

            switch (info.action)
            {
                case PrefabStore.StoreAction.Action.INSTANTIATE:
                    if (!m_avatorInstantiateHistory.ContainsKey(info.userId))
                    {
                        m_avatorInstantiateHistory.Add(info.userId, info);

                        m_avatorStore.UpdateByInstantiateInfo(info, out avator);

                        return true;
                    }

                    return false;
                case PrefabStore.StoreAction.Action.DELETE:
                    if (m_avatorInstantiateHistory.ContainsKey(info.userId))
                    {
                        m_avatorInstantiateHistory.Remove(info.userId);

                        m_avatorStore.UpdateByInstantiateInfo(info, out avator);

                        return true;
                    }

                    return false;
            }

            return false;
        }

        public RoomConfig.CreateOffer GetCreateOffer()
        {
            return config.GetCreateOffer();
        }

        public RoomConfig.DeleteOffer GetDeleteOffer()
        {
            return config.GetDeleteOffer(m_id);
        }

        public IEnumerator CreateRoomAsync(UnityAction<string> callback)
        {
            var url = config.GetUrl() + $"/room/create/{Http.GetBase64(GetCreateOffer())}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Create Room failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var answer = JsonUtility.FromJson<RoomConfig.CreateAnswer>(task.Result);

            m_id = answer.room_id;

            callback.Invoke(task.Result);
        }

        public IEnumerator DeleteRoomAsync(UnityAction<string> callback)
        {
            var url = config.GetUrl() + $"/room/delete/{Http.GetBase64(GetDeleteOffer())}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Delete Room failed, url={url}, err is {task.Exception}");
                yield break;
            }

            callback.Invoke(task.Result);
        }

        public void CreateRoom(MonoBehaviour mono, UnityAction<string> callback)
        {
            mono.StartCoroutine(CreateRoomAsync(callback));
        }

        public void DeleteRoom(MonoBehaviour mono, UnityAction<string> callback)
        {
            mono.StartCoroutine(DeleteRoomAsync(callback));
        }
    }
}
