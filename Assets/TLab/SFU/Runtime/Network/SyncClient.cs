using System.Threading.Tasks;
using System.Collections;
using System;
using System.Text;
using UnityEngine;
using static TLab.SFU.ComponentExtention;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Sync Client (TLab)")]
    public class SyncClient : MonoBehaviour, INetworkEventHandler
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [SerializeField] private Adapter m_adapter;
        [SerializeField] private PrefabStore m_prefabStore;

        [Header("Anchors")]
        [SerializeField] private Transform[] m_instantiateAnchors;
        [SerializeField] private Transform[] m_respownAnchors;

        private static WebSocketClient m_wsClient;
        private static WebRTCClient m_rtcClient;

        private static PhysicsUpdateType m_physicsUpdateType = PhysicsUpdateType.NONE;

        public static PhysicsUpdateType physicsUpdateType => m_physicsUpdateType;

        public delegate void OnMessageCallback(int from, int to, byte[] bytes);
        private static Hashtable m_messageCallbacks = new Hashtable();

        private IEnumerator m_connectTask = null;

        public static SyncClient instance;

        public const int HEADER_SIZE = 4;   // pktId (4)

        public const int PAYLOAD_OFFSET = SfuClient.RECV_PACKET_HEADER_SIZE + HEADER_SIZE;

        public static Adapter adapter
        {
            get
            {
                if (instance == null)
                {
                    return null;
                }

                return instance.m_adapter;
            }
        }

        public static UserAdapter userAdapter
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

        public static RoomAdapter roomAdapter
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

        public static PrefabStore prefabStore
        {
            get
            {
                if (instance == null)
                {
                    return null;
                }

                return instance.m_prefabStore;
            }
        }

        public static bool created
        {
            get
            {
                return instance != null;
            }
        }

        public static bool wsConnected
        {
            get
            {
                if (m_wsClient == null)
                {
                    return false;
                }

                return m_wsClient.connected;
            }
        }

        public static bool wsEnabled
        {
            get
            {
                if (userAdapter == null)
                {
                    return false;
                }

                return userAdapter.regested && wsConnected;
            }
        }

        public static bool rtcConnected
        {
            get
            {
                if (m_rtcClient == null)
                {
                    return false;
                }

                return true;  // TODO: + Connected
            }
        }

        public static bool rtcEnabled
        {
            get
            {
                if (userAdapter == null)
                {
                    return false;
                }

                return userAdapter.regested && rtcConnected;
            }
        }

        public static int userId
        {
            get
            {
                return userAdapter.id;
            }
        }

        public static bool IsOwn(int userId)
        {
            if (userAdapter == null)
            {
                return false;
            }

            return userAdapter.id == userId;
        }

        #region STRUCT

        [System.Serializable]
        public enum PhysicsUpdateType
        {
            NONE,
            SENDER,
            RECEIVER
        };

        public enum DstIndex
        {
            BROADCAST = -1
        };

        #endregion STRUCT

        #region MESSAGE

        [Serializable]
        public struct MSG_Join : IPacketable
        {
            public static int pktId;

            static MSG_Join() => pktId = nameof(MSG_Join).GetHashCode();

            public int messageType; // 0: request, 1: response, 2: broadcast
            public PrefabStore.StoreAction avatorInstantiateInfo;

            // response only
            public Address32[] idAvails;
            public PhysicsUpdateType physicsUpdateType;
            public PrefabStore.StoreAction[] othersInstantiateInfos;

            public byte[] Marshall() => IPacketable.MarshallJson(pktId, this);

            public void UnMarshall(byte[] bytes, out MSG_Join @object) => IPacketable.UnMarshallJson(bytes, out @object);
        }

        [Serializable]
        public struct MSG_PhysicsUpdateType : IPacketable
        {
            public static int pktId;

            static MSG_PhysicsUpdateType() => pktId = nameof(MSG_PhysicsUpdateType).GetHashCode();

            public PhysicsUpdateType physicsUpdateType;

            public byte[] Marshall() => IPacketable.MarshallJson(pktId, this);

            public void UnMarshall(byte[] bytes, out MSG_PhysicsUpdateType @object) => IPacketable.UnMarshallJson(bytes, out @object);
        }

        [Serializable]
        public struct MSG_IdAvails : IPacketable
        {
            public static int pktId;

            static MSG_IdAvails() => pktId = nameof(MSG_IdAvails).GetHashCode();

            public int messageType; // 0: request, 1: response
            public int length;
            public Address32[] idAvails;

            public byte[] Marshall() => IPacketable.MarshallJson(pktId, this);

            public void UnMarshall(byte[] bytes) => IPacketable.UnMarshallJson(bytes, this);
        }

        #endregion MESSAGE

        #region REFLESH

        //public void ForceReflesh(bool reloadWorldData)
        //{
        //    SendWS(action: WebAction.REFLESH, active: reloadWorldData);
        //}

        //public void UniReflesh(string id)
        //{
        //    SendWS(action: WebAction.UNI_REFLESH_TRANSFORM, transform: new WebObjectInfo { id = id });
        //}

        #endregion REFLESH

        public WebTransform GetRespownAnchor(int userId)
        {
            if (userId > m_respownAnchors.Length)
            {
                return new WebTransform();
            }

            var anchor = m_respownAnchors[userId];
            var @transform = new WebTransform(anchor.position, anchor.rotation);

            return @transform;
        }

        public WebTransform GetInstantiateAnchor(int userId)
        {
            if (userId > m_instantiateAnchors.Length)
            {
                return new WebTransform();
            }

            var anchor = m_instantiateAnchors[userId];
            var @transform = new WebTransform(anchor.position, anchor.rotation);

            return @transform;
        }

        public void UpdatePhysicsUpdateType(PhysicsUpdateType physicsUpdateType)
        {
            // TODO
        }

        public static void RegisterOnMessage(int msgId, OnMessageCallback callback)
        {
            if (!m_messageCallbacks.ContainsKey(msgId))
            {
                m_messageCallbacks[msgId] = callback;
            }
        }

        private IEnumerator ConnectTask()
        {
            yield return null;

            CloseWS();

            yield return null;

            #region ADD_CALLBACKS

            RegisterOnMessage(MSG_Join.pktId, (from, to, bytes) =>
            {
                var @object = new MSG_Join();
                @object.UnMarshall(bytes);

                switch (@object.messageType)
                {
                    case 0: // request
                        {
                            var response = new MSG_Join();
                            response.messageType = 1;
                            response.avatorInstantiateInfo = @object.avatorInstantiateInfo;

                            // response
                            response.avatorInstantiateInfo.publicId = UniqueId.Generate();
                            response.idAvails = UniqueId.Generate(5);   // TODO
                            response.physicsUpdateType = PhysicsUpdateType.RECEIVER;
                            response.othersInstantiateInfos = roomAdapter.avatorInstantiateHistorys;

                            roomAdapter.UpdateState(@object.avatorInstantiateInfo, out var avator);

                            SendWS(response.avatorInstantiateInfo.userId, response.Marshall());

                            Foreach<NetworkedObject>((networkedObject) =>
                            {
                                // SyncState
                            });
                        }
                        break;
                    case 1: // response
                        {
                            UpdatePhysicsUpdateType(@object.physicsUpdateType);

                            foreach (var othersInstantiateInfo in @object.othersInstantiateInfos)
                            {
                                roomAdapter.UpdateState(othersInstantiateInfo, out var avator);
                            }

                            Foreach<NetworkedObject>((networkedObject) =>
                            {
                                networkedObject.Init();
                            });
                        }
                        break;
                    case 2: // broadcast
                        {
                            roomAdapter.UpdateState(@object.avatorInstantiateInfo, out var avator);
                        }
                        break;
                }
            });

            RegisterOnMessage(MSG_PhysicsUpdateType.pktId, (from, to, bytes) =>
            {
                var @object = new MSG_Join();
                @object.UnMarshall(bytes);

                UpdatePhysicsUpdateType(@object.physicsUpdateType);
            });

            RegisterOnMessage(MSG_IdAvails.pktId, (from, to, bytes) =>
            {
                var @object = new MSG_Join();
                @object.UnMarshall(bytes);

                switch (@object.messageType)
                {
                    case 0: // request
                        break;
                    case 1: // response
                        break;
                }
            });

#if false
            m_messageCallbacks[(int)WebAction.ACEPT] = (from, obj) => {

                m_id = obj.dstIndex;

                m_guestTable[m_id] = true;

                foreach (var trackTarget in m_trackTargets)
                {
                    var parts = trackTarget.parts;
                    var target = trackTarget.target;

                    var trackerName = GetBodyTrackerID(PREFAB_NAME, m_id, parts);
                    target.name = trackerName;

                    var tracker = target.gameObject.AddComponent<BodyTracker>();
                    tracker.Init(parts, true);
                    tracker.SetSyncEnable(true);

                    CacheAvatorParts(m_id, tracker.gameObject);
                }

                if (m_instantiateAnchor != null)
                {
                    m_playerRoot.SetLocalPositionAndRotation(m_instantiateAnchor.position, m_instantiateAnchor.rotation);
                }
                else
                {
                    m_playerRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                }

                var anchor = m_respownAnchors[m_id];
                m_playerRoot.SetPositionAndRotation(anchor.position, anchor.rotation);

                m_rtcClient.Join(GetClientID(m_id), m_roomConfig.id);

            };
            m_messageCallbacks[(int)WebAction.GUEST_DISCONNECT] = (from, obj) => {

                int index = obj.srcIndex;

                if (!m_guestTable[index])
                {
                    return;
                }

                DeleteAvator(index);

                m_guestTable[index] = false;

                foreach (var callback in m_customCallbacks)
                {
                    callback.OnGuestDisconnected(index);
                }

                Debug.Log(THIS_NAME + "Guest disconncted: " + index.ToString());

            };
            m_messageCallbacks[(int)WebAction.GUEST_PARTICIPATION] = (from, obj) => {

                var index = obj.srcIndex;

                if (m_guestTable[index])
                {
                    Debug.LogError(THIS_NAME + $"Guest already exists: {index}");
                    return;
                }

                CloneAvator(index, m_avatorConfig);

                m_guestTable[index] = true;

                foreach (var callback in m_customCallbacks)
                {
                    callback.OnGuestParticipated(index);
                }

                Debug.Log(THIS_NAME + $"Guest participated: {index}");

                return;
            };
#endif
            #endregion ADD_CALLBACKS

            m_wsClient = WebSocketClient.Open(this, m_adapter, "master", OnMessage, (OnOpen, OnOpen), (OnClose, OnClose), OnError);

            m_connectTask = null;

            yield return 1;
        }

        public void Connect()
        {
            m_connectTask = ConnectTask();
        }

        #region WS

        public Task SendWS(int to, byte[] bytes)
        {
            if (wsEnabled)
                return m_wsClient.Send(to, bytes);

            return new Task(() => { });
        }

        public Task SendWS(int to, string message) => SendWS(to, Encoding.UTF8.GetBytes(message));

        public Task SendWS(byte[] bytes) => SendWS(userAdapter.id, bytes);

        public async void CloseWS()
        {
            await m_wsClient?.HangUp();
            m_wsClient = null;
        }

        #endregion WS

        #region RTC

        public void SendRTC(int to, byte[] bytes) => m_rtcClient?.Send(to, bytes);

        public void SendRTC(byte[] bytes) => SendRTC(userAdapter.id, bytes);

        public void SendRTC(int to, string text) => m_rtcClient?.Send(to, text);

        public void SendRTC(string text) => SendRTC(userAdapter.id, text);

        public void CloseRTC()
        {
            m_rtcClient?.HangUp();
            m_rtcClient = null;
        }

        #endregion RTC

        void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            Connect();
        }

        private void Update()
        {
            m_connectTask?.MoveNext();
        }

        private void OnDestroy()
        {
            CloseRTC();
            CloseWS();
        }

        private void OnApplicationQuit()
        {
            CloseRTC();
            CloseWS();
        }

        public void OnMessage(int from, int to, byte[] bytes)
        {
            var msgTyp = bytes[0];

            if (m_messageCallbacks.ContainsKey(msgTyp))
            {
                var callback = m_messageCallbacks[msgTyp] as OnMessageCallback;
                callback.Invoke(from, to, bytes);
            }
        }

        public void OnOpen()
        {
            throw new NotImplementedException();
        }

        public void OnClose()
        {
            throw new NotImplementedException();
        }

        public void OnOpen(int from)
        {
            throw new NotImplementedException();
        }

        public void OnClose(int from)
        {
            throw new NotImplementedException();
        }

        public void OnError()
        {
            throw new NotImplementedException();
        }
    }
}
