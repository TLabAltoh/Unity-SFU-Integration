using UnityEngine;
using UnityEngine.UI;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    public class AdapterSample : MonoBehaviour
    {
        [SerializeField] private Adapter m_adapter;

        private ScrollRect m_scrollRect;
        private Transform m_scrollViewContent;

        private bool m_forceScrollToTail = true;

        public enum State
        {
            NONE,
            CONNECTING,
            CONNECTED
        };

        private static State m_state;

        public static State state => m_state;

        public Adapter adapter => m_adapter;

        public RoomAdapter roomAdapter
        {
            get
            {
                if (adapter == null)
                {
                    return null;
                }

                return adapter.room;
            }
        }

        public UserAdapter userAdapter
        {
            get
            {
                if (adapter == null)
                {
                    return null;
                }

                return adapter.user;
            }
        }

        public static AdapterSample instance;

        public string THIS_NAME => "[" + this.GetType() + "] ";

        private void Awake()
        {
            instance = this;
        }

        public Adapter GetClone()
        {
            return m_adapter.GetClone();
        }

        public void OnMessage(string message)
        {
            var messageChunk = Instantiate(Resources.Load<GameObject>("Sample/Message"));

            messageChunk.transform.SetParent(m_scrollViewContent);
            messageChunk.GetComponent<MessageChunk>()?.InitMessage(message);
        }

        public void OnConnect(string message)
        {
            OnMessage(message);

            m_state = State.CONNECTED;
        }

        public void CreateRoom()
        {
            m_state = State.CONNECTING;

            adapter.CreateRoom(this, OnConnect);
        }

        public void DeleteRoom()
        {
            adapter.DeleteRoom(this, OnMessage);
        }

        private void Start()
        {
            m_scrollRect = GetComponentInChildren<ScrollRect>();
            m_scrollViewContent = m_scrollRect.transform.Find("Viewport/Content");

            m_scrollRect.onValueChanged.AddListener((value) =>
            {
                if (UnityEngine.Input.GetMouseButton(0))
                {
                    m_forceScrollToTail = (value.y < 0.1f);
                }
            });
        }

        private void Update()
        {
            if (m_forceScrollToTail && !UnityEngine.Input.GetMouseButton(0))
            {
                m_scrollRect.verticalNormalizedPosition = 0.0f;
            }
        }
    }
}
