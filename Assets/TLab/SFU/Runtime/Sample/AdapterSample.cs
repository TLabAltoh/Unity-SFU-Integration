using UnityEngine;
using UnityEngine.UI;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    public class AdapterSample : MonoBehaviour
    {
        public static bool local => false;

        [SerializeField] private ScrollRect m_scrollRect;
        [SerializeField] private Adapter m_adapter;

        private Transform m_scrollViewContent;

        private bool m_scrollViewAvailable = false;
        private bool m_forceScrollToTail = true;

        public enum State
        {
            NONE,
            CREATED,
            DELETED,
        };

        private static State m_state;

        public static State state => m_state;

        public Adapter adapter => m_adapter;

        public static AdapterSample instance;

        public string THIS_NAME => "[" + this.GetType() + "] ";

        private void Awake() => instance = this;

        public Adapter GetClone() => m_adapter.GetClone();

        public void OnMessage(string message)
        {
            if (m_scrollViewAvailable)
            {
                var messageChunk = Instantiate(Resources.Load<GameObject>("Sample/Message"));

                messageChunk.transform.SetParent(m_scrollViewContent);
                messageChunk.GetComponent<MessageChunk>()?.InitMessage(message);
            }
            else
            {
                Debug.Log("OnMessage: " + message);
            }
        }

        public void GetFirstRoom() => m_adapter.GetInfo(this, (response) => {
            var @object = JsonUtility.FromJson<Network.Answer.Infos>(response);
            var roomId = @object.room_infos[0].room_id;
            m_adapter.Init(m_adapter.config, roomId, m_adapter.key, m_adapter.masterKey);
            OnMessage(response);
        });

        public void Create() => adapter.Create(this, (response) => {
            m_state = State.CREATED;
            OnMessage(response);
        });

        public void Delete() => adapter.Delete(this, (response) => {
            m_state = State.DELETED;
            OnMessage(response);
        });

        private void Start()
        {
            if (m_scrollRect == null)
                return;

            m_scrollViewContent = m_scrollRect.transform.Find("Viewport/Content");

            m_scrollRect.onValueChanged.AddListener((value) =>
            {
                if (UnityEngine.Input.GetMouseButton(0))
                    m_forceScrollToTail = (value.y < 0.1f);
            });

            m_scrollViewAvailable = true;
        }

        private void Update()
        {
            if (m_scrollViewAvailable && m_forceScrollToTail && !UnityEngine.Input.GetMouseButton(0))
                m_scrollRect.verticalNormalizedPosition = 0.0f;
        }
    }
}
