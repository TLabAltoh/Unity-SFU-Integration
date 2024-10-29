using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU.Network
{
    [CreateAssetMenu(fileName = "User Adapter", menuName = "TLab/SFU/User Adapter")]
    public class UserAdapter : ScriptableObject
    {
        public enum UserId
        {
            NOT_REGISTED = -1,
        }

        [SerializeField] private UserConfig m_config;

        private int m_id;
        private uint m_token;

        public UserConfig config => m_config;

        public int id => m_id;

        public uint token => m_token;

        public void Init(UserConfig config)
        {
            m_config = config;
        }

        public UserAdapter GetClone()
        {
            var instance = CreateInstance<UserAdapter>();

            instance.Init(m_config);

            return instance;
        }

        public bool regested => m_id != (int)UserId.NOT_REGISTED;

        public Offer.JoinRoom GetJoinRoom(RoomAdapter adapter, string roomKey, string masterKey = "")
        {
            return new Offer.JoinRoom
            {
                user_name = name,
                room_id = adapter.id,
                room_key = roomKey,
                master_key = masterKey,
            };
        }

        public Offer.ExitRoom GetExitRoom(RoomAdapter adapter, string roomKey)
        {
            return new Offer.ExitRoom
            {
                room_id = adapter.id,
                room_key = roomKey,
                user_id = m_id,
                user_token = m_token
            };
        }

        public IEnumerator JoinRoomAsync(RoomAdapter adapter, string roomKey, string masterKey, UnityAction<string> callback)
        {
            var url = adapter.config.GetUrl() + $"/room/join/{Http.GetBase64(GetJoinRoom(adapter, roomKey, masterKey))}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Join failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var answer = JsonUtility.FromJson<Answer.JoinRoom>(task.Result);

            m_id = answer.user_id;
            m_token = answer.user_token;

            callback.Invoke(task.Result);
        }

        public IEnumerator ExitRoomAsync(RoomAdapter adapter, string roomKey, UnityAction<string> callback)
        {
            var url = adapter.config.GetUrl() + $"/room/exit/{Http.GetBase64(GetExitRoom(adapter, roomKey))}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Exit failed, url={url}, err is {task.Exception}");
                yield break;
            }

            callback.Invoke(task.Result);
        }

        public void JoinRoom(RoomAdapter adapter, string roomKey, string masterKey, MonoBehaviour mono, UnityAction<string> callback)
        {
            mono.StartCoroutine(JoinRoomAsync(adapter, roomKey, masterKey, callback));
        }

        public void ExitRoom(RoomAdapter adapter, string roomKey, MonoBehaviour mono, UnityAction<string> callback)
        {
            mono.StartCoroutine(ExitRoomAsync(adapter, roomKey, callback));
        }
    }
}
