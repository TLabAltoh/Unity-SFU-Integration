using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using static TLab.SFU.ComponentExtension;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Network Client (TLab)")]
    public class NetworkClient : MonoBehaviour, INetworkConnectionEventHandler
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [SerializeField] private Adapter m_adapter;
        [SerializeField] private PrefabShop m_avatorShop;

        [SerializeField] private UnityEvent<string> m_onLog;

        private static WebSocketClient m_wsClient;
        private static WebRTCClient m_rtcClient;

        public static Queue<Address32> idAvails = new Queue<Address32>();

        private Dictionary<int, PrefabStore.StoreAction> m_avatorHistory = new Dictionary<int, PrefabStore.StoreAction>();
        public PrefabStore.StoreAction[] avatorHistorys => m_avatorHistory.Values.ToArray();

        private IEnumerator m_connectTask = null;

        public static NetworkClient instance;

        private static PhysicsRole m_physicsRole = PhysicsRole.NONE;
        public static PhysicsRole physicsRole => m_physicsRole;

        public delegate void OnMessageCallback(int from, int to, byte[] bytes);
        private static Hashtable m_messageCallbacks = new Hashtable();

        private static List<(UnityAction, UnityAction<int>)> m_onJoin = new List<(UnityAction, UnityAction<int>)>();
        private static List<(UnityAction, UnityAction<int>)> m_onExit = new List<(UnityAction, UnityAction<int>)>();

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

                return m_rtcClient.connected;
            }
        }

        public static bool rtcEnabled => adapter.regested && rtcConnected;

        public static int userId => adapter.userId;

        public static bool IsOwn(int userId) => adapter.userId == userId;

        #region STRUCT

        [System.Serializable]
        public enum PhysicsRole
        {
            NONE,
            SEND,
            RECV
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

            public MSG_Join() : base() { }

            public MSG_Join(byte[] bytes) : base(bytes) { }

            public int messageType; // 0: request, 1: response, 2: broadcast
            public PrefabStore.StoreAction avatorAction;

            // response only
            public Address32[] idAvails;
            public PhysicsRole physicsRole;
            public PrefabStore.StoreAction[] othersHistory;
        }

        [Serializable]
        public class MSG_PhysicsRole : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_PhysicsRole() => pktId = MD5From(nameof(MSG_PhysicsRole));

            public MSG_PhysicsRole() : base() { }

            public MSG_PhysicsRole(byte[] bytes) : base(bytes) { }

            public PhysicsRole physicsRole;
        }

        [Serializable]
        public class MSG_IdAvails : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_IdAvails() => pktId = MD5From(nameof(MSG_IdAvails));

            public MSG_IdAvails() : base() { }

            public MSG_IdAvails(byte[] bytes) : base(bytes) { }

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

        public void SetPhysicsRole(PhysicsRole physicsRole)
        {
            m_physicsRole = physicsRole;
            Foreach<NetworkTransform>((t) => t.OnPhysicsRoleChange());
        }

        public static void RegisterOnJoin(UnityAction callback0, UnityAction<int> callback1)
        {
            var @object = (callback0, callback1);
            if (!m_onJoin.Contains(@object))
                m_onJoin.Add(@object);
        }

        public static void UnRegisterOnJoin(UnityAction callback0, UnityAction<int> callback1)
        {
            var @object = (callback0, callback1);
            if (m_onJoin.Contains(@object))
                m_onJoin.Remove(@object);
        }

        public static void RegisterOnExit(UnityAction callback0, UnityAction<int> callback1)
        {
            var @object = (callback0, callback1);
            if (!m_onExit.Contains(@object))
                m_onExit.Add(@object);
        }

        public static void UnRegisterOnExit(UnityAction callback0, UnityAction<int> callback1)
        {
            var @object = (callback0, callback1);
            if (m_onExit.Contains(@object))
                m_onExit.Remove(@object);
        }

        public static void RegisterOnMessage(int msgId, OnMessageCallback callback)
        {
            if (!m_messageCallbacks.ContainsKey(msgId))
                m_messageCallbacks[msgId] = callback;
        }

        public static void UnRegisterOnMessage(int msgId)
        {
            if (m_messageCallbacks.ContainsKey(msgId))
                m_messageCallbacks.Remove(msgId);
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
                var @object = new MSG_Join(bytes);

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
                            response.physicsRole = PhysicsRole.RECV;
                            response.othersHistory = avatorHistorys;

                            SendWS(response.avatorAction.userId, response.Marshall());
                        }
                        break;
                    case 1: // response
                        {
                            SetPhysicsRole(@object.physicsRole);

                            foreach (var action in @object.othersHistory)
                                UpdateState(action, out var avator);

                            Foreach<NetworkObject>((t) => t.Init());

                            m_onJoin.ForEach((c) => c.Item1.Invoke());
                        }
                        break;
                    case 2: // broadcast
                        {
                            UpdateState(@object.avatorAction, out var avator);

                            if (userId == 0)
                                Foreach<NetworkObject>((t) => t.SyncViaWebSocket());

                            m_onJoin.ForEach((c) => c.Item2.Invoke(from));
                        }
                        break;
                }
            });

            RegisterOnMessage(MSG_PhysicsRole.pktId, (from, to, bytes) =>
            {
                var @object = new MSG_PhysicsRole(bytes);

                SetPhysicsRole(@object.physicsRole);
            });

            RegisterOnMessage(MSG_IdAvails.pktId, (from, to, bytes) =>
            {
                var @object = new MSG_IdAvails(bytes);

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
#endif
            #endregion ADD_CALLBACKS

            m_wsClient = WebSocketClient.Open(this, m_adapter, "master", OnMessage, (OnOpen, OnOpen), (OnClose, OnClose), OnError);

            m_connectTask = null;

            yield break;
        }

        public void Join() => m_adapter.GetInfo(this, (@string) =>
        {
            m_onLog.Invoke(@string);
            var @object = JsonUtility.FromJson<Answer.Infos>(@string);
            if (@object.room_infos.Length == 0)
            {
                m_adapter.Create(this, (@string) =>
                {
                    m_onLog.Invoke(@string);
                    m_adapter.Join(this, (@string) => {
                        m_onLog.Invoke(@string);
                        m_connectTask = ConnectTask();
                    });
                });
            }
            else
            {
                var roomId = @object.room_infos[0].room_id;
                m_adapter.Init(m_adapter.config, roomId, m_adapter.key, m_adapter.masterKey);

                m_adapter.Join(this, (@string) => {
                    m_onLog.Invoke(@string);
                    m_connectTask = ConnectTask();
                });
            }
        });

        public void Exit()
        {
            m_rtcClient?.HangUp();
            m_adapter.Exit(this, (@string) => {
                m_onLog.Invoke(@string);
                m_onExit.ForEach((c) => c.Item1.Invoke());
            });
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
            m_rtcClient = WebRTCClient.Whep(this, m_adapter, "stream", (from, to, bytes) => Debug.Log("OnMessage: "), (() => Debug.Log("OnOpen !"), (_) => { }), (() => Debug.Log("OnClose !"), (_) => { }), () => Debug.LogError("[RTCClient] Error !"), new Unity.WebRTC.RTCDataChannelInit(), false, false, null);

            if (userId == 0)
            {
                SetPhysicsRole(PhysicsRole.SEND);

                Foreach<NetworkObject>((t) => t.Init());

                if (m_avatorShop.GetAnchor(0, out var anchor))
                {
                    var action = new PrefabStore.StoreAction()
                    {
                        action = PrefabStore.StoreAction.Action.INSTANTIATE,
                        elemId = 0,
                        userId = 0,
                        publicId = UniqueId.Generate(),
                        transform = anchor,
                    };

                    UpdateState(action, out var avator);
                }
            }
            else
            {
                if (m_avatorShop.GetAnchor(userId, out var anchor))
                {
                    var action = new PrefabStore.StoreAction()
                    {
                        action = PrefabStore.StoreAction.Action.INSTANTIATE,
                        elemId = 0,
                        userId = userId,
                        transform = anchor,
                    };

                    var @object = new MSG_Join()
                    {
                        messageType = 0,
                        avatorAction = action,
                    };

                    SendWS(0, @object.Marshall());
                }
            }
        }

        public void OnClose() => m_onExit.ForEach((c) => c.Item1.Invoke());

        public void OnOpen(int from)
        {
            throw new NotImplementedException();
        }

        public void OnClose(int from) => m_onExit.ForEach((c) => c.Item2.Invoke(from));

        public void OnError()
        {
            throw new NotImplementedException();
        }
    }
}
