using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU.Network
{
    [CreateAssetMenu(fileName = "Adapter", menuName = "TLab/SFU/Adapter")]
    public class Adapter : ScriptableObject
    {
        [SerializeField] private Config m_config;

        [SerializeField] private string m_key;

        [SerializeField] private string m_masterKey;

        private int m_roomId;

        private int m_userId;

        private uint m_token;

        public int roomId => m_roomId;

        public int userId => m_userId;

        public uint token => m_token;

        public string key => m_key;

        public string masterKey => m_masterKey;

        public Config config => m_config;

        public enum UserId
        {
            NOT_REGISTED = -1,
        }

        public bool regested => m_userId != (int)UserId.NOT_REGISTED;

        public void Init(Config config, int roomId, string key, string masterKey)
        {
            m_config = config;

            m_roomId = roomId;

            m_key = key;

            m_masterKey = masterKey;
        }

        public void Init(Config config, int roomId, int userId, uint token, string key, string masterKey)
        {
            Init(config, roomId, key, masterKey);

            m_userId = userId;

            m_token = token;
        }

        public Adapter GetClone()
        {
            var instance = CreateInstance<Adapter>();

            instance.Init(m_config, m_roomId, m_userId, m_token, m_key, m_masterKey);

            return instance;
        }

        public IEnumerator GetInfoAsync(UnityAction<string> callback)
        {
            var url = config.GetUrl() + $"/room";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"Adapter: Enum  failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var answer = JsonUtility.FromJson<Answer.Create>(task.Result);

            m_roomId = answer.room_id;

            m_config.GetAuth(out m_key, out m_masterKey);

            callback.Invoke(task.Result);
        }

        public IEnumerator CreateAsync(UnityAction<string> callback)
        {
            var url = m_config.GetUrl() + $"/room/create/{Http.GetBase64(m_config.GetCreate())}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"Adapter: Create  failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var answer = JsonUtility.FromJson<Answer.Create>(task.Result);

            m_roomId = answer.room_id;

            m_config.GetAuth(out m_key, out m_masterKey);

            callback.Invoke(task.Result);
        }

        public IEnumerator DeleteAsync(UnityAction<string> callback)
        {
            var @object = new Offer.Delete()
            {
                room_id = roomId,
                master_key = masterKey,
            };

            var url = m_config.GetUrl() + $"/room/delete/{Http.GetBase64(@object)}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"Adapter: Delete  failed, url={url}, err is {task.Exception}");
                yield break;
            }

            callback.Invoke(task.Result);
        }

        public void GetInfo(MonoBehaviour mono, UnityAction<string> callback) => mono.StartCoroutine(GetInfoAsync(callback));

        public void Create(MonoBehaviour mono, UnityAction<string> callback) => mono.StartCoroutine(CreateAsync(callback));

        public void Delete(MonoBehaviour mono, UnityAction<string> callback) => mono.StartCoroutine(DeleteAsync(callback));

        public IEnumerator JoinAsync(UnityAction<string> callback)
        {
            var @object = new Offer.Join
            {
                user_name = m_config.name,
                room_id = m_roomId,
                room_key = m_key,
                master_key = m_masterKey,
            };

            var url = m_config.GetUrl() + $"/room/join/{Http.GetBase64(@object)}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Join failed, url={url}, err is {task.Exception}");
                yield break;
            }

            var answer = JsonUtility.FromJson<Answer.Join>(task.Result);

            m_userId = answer.user_id;
            m_token = answer.user_token;

            callback.Invoke(task.Result);
        }

        public IEnumerator ExitAsync(UnityAction<string> callback)
        {
            var @object = new Offer.Exit
            {
                room_id = m_roomId,
                room_key = m_key,
                user_id = m_userId,
                user_token = m_token
            };

            var url = m_config.GetUrl() + $"/room/exit/{Http.GetBase64(@object)}/";

            var task = Http.GetResponse(url);

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Exit failed, url={url}, err is {task.Exception}");
                yield break;
            }

            callback.Invoke(task.Result);
        }

        public void Join(MonoBehaviour mono, UnityAction<string> callback) => mono.StartCoroutine(JoinAsync(callback));

        public void Exit(MonoBehaviour mono, UnityAction<string> callback) => mono.StartCoroutine(ExitAsync(callback));

        public RequestAuth GetRequestAuth() => new RequestAuth(m_roomId, m_key, m_userId, m_token);
    }
}
