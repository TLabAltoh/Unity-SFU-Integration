using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.NetworkedVR.Network
{
    [CreateAssetMenu(fileName = "User Adapter", menuName = "TLab/NetworkedVR/User Adapter")]
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

        public UserConfig.JoinOffer GetJoinOffer(RoomAdapter roomAdapter)
        {
            var createOffer = roomAdapter.GetCreateOffer();

            return new UserConfig.JoinOffer
            {
                user_name = name,
                room_id = roomAdapter.id,
                room_pass = createOffer.room_pass,
                master_key = createOffer.master_key,
            };
        }

        public UserConfig.ExitOffer GetExitOffer(RoomAdapter roomAdapter)
        {
            var createOffer = roomAdapter.GetCreateOffer();

            return new UserConfig.ExitOffer
            {
                room_id = roomAdapter.id,
                room_pass = createOffer.room_pass,
                user_id = m_id,
                user_token = m_token
            };
        }

        public IEnumerator JoinRoomAsync(RoomAdapter roomAdapter, UnityAction<string> callback)
        {
            var url = roomAdapter.config.address + $"/room/join/{Http.GetBase64(GetJoinOffer(roomAdapter))}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Join failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var answer = JsonUtility.FromJson<UserConfig.JoinAnswer>(task.Result);

            m_id = answer.user_id;
            m_token = answer.user_token;

            callback.Invoke(task.Result);
        }

        public IEnumerator ExitRoomAsync(RoomAdapter roomAdapter, UnityAction<string> callback)
        {
            var url = roomAdapter.config.address + $"/room/exit/{Http.GetBase64(GetExitOffer(roomAdapter))}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Exit failed, url={url}, err is {task.Exception}");
                yield break;
            }

            callback.Invoke(task.Result);
        }

        public void JoinRoom(RoomAdapter roomAdapter, MonoBehaviour mono, UnityAction<string> callback)
        {
            mono.StartCoroutine(JoinRoomAsync(roomAdapter, callback));
        }

        public void ExitRoom(RoomAdapter roomAdapter, MonoBehaviour mono, UnityAction<string> callback)
        {
            mono.StartCoroutine(ExitRoomAsync(roomAdapter, callback));
        }
    }
}
