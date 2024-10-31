using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System;
using System.Linq;
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
        [SerializeField] private PrefabShop m_avatorShop;

        private static WebSocketClient m_wsClient;
        private static WebRTCClient m_rtcClient;

        public static Queue<Address32> idAvails = new Queue<Address32>();

        private Dictionary<int, PrefabStore.StoreAction> m_avatorHistory = new Dictionary<int, PrefabStore.StoreAction>();
        public PrefabStore.StoreAction[] avatorHistorys => m_avatorHistory.Values.ToArray();

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
                    return null;

                return instance.m_adapter;
            }
        }

        public static bool created => instance != null;

        public static bool wsConnected
        {
            get
            {
                if (m_wsClient == null)
                    return false;

                return m_wsClient.connected;
            }
        }

        public static bool wsEnabled => adapter.regested && wsConnected;

        public static bool rtcConnected
        {
            get
            {
                if (m_rtcClient == null)
                    return false;

                return true;  // TODO: + Connected
            }
        }

        public static bool rtcEnabled => adapter.regested && rtcConnected;

        public static int userId => adapter.userId;

        public static bool IsOwn(int userId) => adapter.userId == userId;

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
        public class MSG_Join : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_Join() => pktId = MD5From(nameof(MSG_Join));

            public int messageType; // 0: request, 1: response, 2: broadcast
            public PrefabStore.StoreAction avatorAction;

            // response only
            public Address32[] idAvails;
            public PhysicsUpdateType physicsUpdateType;
            public PrefabStore.StoreAction[] othersHistory;
        }

        [Serializable]
        public class MSG_PhysicsUpdateType : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_PhysicsUpdateType() => pktId = MD5From(nameof(MSG_PhysicsUpdateType));

            public PhysicsUpdateType physicsUpdateType;
        }

        [Serializable]
        public class MSG_IdAvails : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_IdAvails() => pktId = MD5From(nameof(MSG_IdAvails));

            public int messageType; // 0: request, 1: response
            public int length;
            public Address32[] idAvails;
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

        public void UpdatePhysicsUpdateType(PhysicsUpdateType physicsUpdateType)
        {
            // TODO:
        }

        public static void RegisterOnMessage(int msgId, OnMessageCallback callback)
        {
            if (!m_messageCallbacks.ContainsKey(msgId))
                m_messageCallbacks[msgId] = callback;
        }

        public bool IsPlayerJoined(int index) => m_avatorHistory.ContainsKey(index);

        public bool GetInstantiateInfo(int index, out PrefabStore.StoreAction info)
        {
            if (m_avatorHistory.ContainsKey(index))
            {
                info = m_avatorHistory[index];
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
                    if (!m_avatorHistory.ContainsKey(info.userId))
                    {
                        m_avatorHistory.Add(info.userId, info);

                        m_avatorShop.store.UpdateByInstantiateInfo(info, out avator);

                        return true;
                    }

                    return false;
                case PrefabStore.StoreAction.Action.DELETE:
                    if (m_avatorHistory.ContainsKey(info.userId))
                    {
                        m_avatorHistory.Remove(info.userId);

                        m_avatorShop.store.UpdateByInstantiateInfo(info, out avator);

                        return true;
                    }

                    return false;
            }

            return false;
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
                            response.avatorAction = @object.avatorAction;

                            // response
                            response.avatorAction.publicId = UniqueId.Generate();
                            response.idAvails = UniqueId.Generate(5);   // TODO
                            response.physicsUpdateType = PhysicsUpdateType.RECEIVER;
                            response.othersHistory = avatorHistorys;

                            SendWS(response.avatorAction.userId, response.Marshall());
                        }
                        break;
                    case 1: // response
                        {
                            UpdatePhysicsUpdateType(@object.physicsUpdateType);

                            foreach (var othersInstantiateInfo in @object.othersHistory)
                                UpdateState(othersInstantiateInfo, out var avator);

                            Foreach<NetworkedObject>((networkedObject) => networkedObject.Init());
                        }
                        break;
                    case 2: // broadcast
                        {
                            UpdateState(@object.avatorAction, out var avator);

                            if (userId == 0)
                                Foreach<NetworkedObject>((networkedObject) => networkedObject.SyncViaWebSocket());
                        }
                        break;
                }
            });

            RegisterOnMessage(MSG_PhysicsUpdateType.pktId, (from, to, bytes) =>
            {
                var @object = new MSG_PhysicsUpdateType();
                @object.UnMarshall(bytes);

                UpdatePhysicsUpdateType(@object.physicsUpdateType);
            });

            RegisterOnMessage(MSG_IdAvails.pktId, (from, to, bytes) =>
            {
                var @object = new MSG_IdAvails();
                @object.UnMarshall(bytes);

                switch (@object.messageType)
                {
                    case 0: // request
                        @object.idAvails = UniqueId.Generate(@object.length);
                        SendWS(@object.Marshall());
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

            yield break;
        }

        public void Join() => m_adapter.Join(this, (@string) => {
            Debug.Log("Join: Success");
            m_connectTask = ConnectTask();
        });

        public void Exit()
        {
            m_rtcClient?.HangUp();
            m_adapter.Exit(this, (@string) => Debug.Log("Exit: Success"));
        }

        #region WS

        public Task SendWS(int to, byte[] bytes)
        {
            if (wsEnabled)
                return m_wsClient.Send(to, bytes);

            return new Task(() => { });
        }

        public Task SendWS(int to, string message) => SendWS(to, Encoding.UTF8.GetBytes(message));

        public Task SendWS(byte[] bytes) => SendWS(userId, bytes);

        public async void CloseWS()
        {
            if (m_wsClient != null)
                await m_wsClient.HangUp();

            m_wsClient = null;
        }

        #endregion WS

        #region RTC

        public void SendRTC(int to, byte[] bytes) => m_rtcClient?.Send(to, bytes);

        public void SendRTC(byte[] bytes) => SendRTC(userId, bytes);

        public void SendRTC(int to, string text) => m_rtcClient?.Send(to, text);

        public void SendRTC(string text) => SendRTC(userId, text);

        public void CloseRTC()
        {
            m_rtcClient?.HangUp();
            m_rtcClient = null;
        }

        #endregion RTC

        void Awake() => instance = this;

        private void Update() => m_connectTask?.MoveNext();

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
            m_rtcClient = WebRTCClient.Whep(this, m_adapter, "stream", (from, to, bytes) => Debug.Log("OnMessage: "), (() => Debug.Log("[RTCClient] Open !"), (_) => { }), (() => Debug.Log("[RTCClient] Close !"), (_) => { }), () => Debug.LogError("[RTCClient] Error !"), new Unity.WebRTC.RTCDataChannelInit(), false, false, null);

            if (userId == 0)
            {
                UpdatePhysicsUpdateType(PhysicsUpdateType.SENDER);

                Foreach<NetworkedObject>((networkedObject) => networkedObject.Init());

                var action = new PrefabStore.StoreAction()
                {
                    action = PrefabStore.StoreAction.Action.INSTANTIATE,
                    elemId = 0,
                    userId = 0,
                    publicId = UniqueId.Generate(),
                    transform = m_avatorShop.GetAnchor(0),
                };

                UpdateState(action, out var avator);
            }
            else
            {
                var action = new PrefabStore.StoreAction()
                {
                    action = PrefabStore.StoreAction.Action.INSTANTIATE,
                    elemId = userId,
                    userId = userId,
                    transform = m_avatorShop.GetAnchor(userId),
                };

                var @object = new MSG_Join()
                {
                    messageType = 0,
                    avatorAction = action,
                };

                SendWS(0, @object.Marshall());
            }
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
