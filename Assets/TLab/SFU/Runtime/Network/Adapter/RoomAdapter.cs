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

        [SerializeField] private string m_key;

        [SerializeField] private string m_masterKey;

        private Dictionary<int, PrefabStore.StoreAction> m_avatorInstantiateHistory = new Dictionary<int, PrefabStore.StoreAction>();

        private int m_id;

        public int id => m_id;

        public string key => m_key;

        public string masterKey => m_masterKey;

        public RoomConfig config => m_config;

        public PrefabStore avatorStore => m_avatorStore;

        public PrefabStore.StoreAction[] avatorInstantiateHistorys => m_avatorInstantiateHistory.Values.ToArray();

        public void Init(int id, string key, string masterKey)
        {
            m_id = id;

            m_key = key;

            m_masterKey = masterKey;
        }

        public void Init(RoomConfig config, int id, string key, string masterKey)
        {
            Init(id, key, masterKey);

            m_config = config;
        }

        public RoomAdapter GetClone()
        {
            var instance = CreateInstance<RoomAdapter>();

            instance.Init(m_config, m_id, m_key, m_masterKey);

            return instance;
        }

        public bool IsPlayerJoined(int index) => m_avatorInstantiateHistory.ContainsKey(index);

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

        public Offer.CreateRoom GetCreateRoom() => config.GetCreateRoom();

        public Offer.DeleteRoom GetDeleteRoom() => config.GetDeleteRoom(m_id, m_masterKey);

        public IEnumerator GetRoomInfoAsync(UnityAction<string> callback)
        {
            var url = config.GetUrl() + $"/room";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"RoomAdapter: Enum Room failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var answer = JsonUtility.FromJson<Answer.CreateRoom>(task.Result);

            m_id = answer.room_id;

            m_config.GetAuth(out m_key, out m_masterKey);

            callback.Invoke(task.Result);
        }

        public IEnumerator CreateRoomAsync(UnityAction<string> callback)
        {
            var url = config.GetUrl() + $"/room/create/{Http.GetBase64(GetCreateRoom())}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"RoomAdapter: Create Room failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var answer = JsonUtility.FromJson<Answer.CreateRoom>(task.Result);

            m_id = answer.room_id;

            m_config.GetAuth(out m_key, out m_masterKey);

            callback.Invoke(task.Result);
        }

        public IEnumerator DeleteRoomAsync(UnityAction<string> callback)
        {
            var url = config.GetUrl() + $"/room/delete/{Http.GetBase64(GetDeleteRoom())}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"RoomAdapter: Delete Room failed, url={url}, err is {task.Exception}");
                yield break;
            }

            callback.Invoke(task.Result);
        }

        public void GetRoomInfo(MonoBehaviour mono, UnityAction<string> callback)
        {
            mono.StartCoroutine(GetRoomInfoAsync(callback));
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
