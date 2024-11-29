using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using static System.BitConverter;
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

        [Serializable]
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

            public enum MessageType
            {
                REQUEST = 0,
                RESPONSE = 1,
                BROADCAST = 2,
            };

            public MSG_Join(MessageType messageType, PrefabStore.StoreAction avatorAction) : base()
            {
                this.messageType = messageType;
                this.avatorAction = avatorAction;
            }

            public MSG_Join(MessageType messageType, PrefabStore.StoreAction avatorAction, Address32[] idAvails, PhysicsRole physicsRole, PrefabStore.StoreAction[] othersHistory) : this(messageType, avatorAction)
            {
                this.idAvails = idAvails;
                this.physicsRole = physicsRole;
                this.othersHistory = othersHistory;
            }

            public MSG_Join(byte[] bytes) : base(bytes) { }

            public MessageType messageType;
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

            public MSG_PhysicsRole(PhysicsRole physicsRole) : base()
            {
                this.physicsRole = physicsRole;
            }

            public MSG_PhysicsRole(byte[] bytes) : base(bytes) { }

            public PhysicsRole physicsRole;
        }

        [Serializable]
        public class MSG_IdAvails : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_IdAvails() => pktId = MD5From(nameof(MSG_IdAvails));

            public MSG_IdAvails(MessageType messageType, int length, Address32[] idAvails) : base()
            {
                this.messageType = messageType;
                this.length = length;
                this.idAvails = idAvails;
            }

            public MSG_IdAvails(byte[] bytes) : base(bytes) { }

            public enum MessageType
            {
                REQUEST = 0,
                RESPONSE = 1,
            };

            public MessageType messageType;
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

        private bool UpdateAvatorState(PrefabStore.StoreAction info, out GameObject avator)
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
                case PrefabStore.StoreAction.Action.DELETE_BY_USER_ID:
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
                    case MSG_Join.MessageType.REQUEST:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.REQUEST));

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.RESPONSE, @object.avatorAction.UpdatePublicId(UniqueId.Generate()), UniqueId.Generate(5), PhysicsRole.RECV, avatorHistorys).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.RESPONSE:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.RESPONSE));

                            SetPhysicsRole(@object.physicsRole);

                            foreach (var action in @object.othersHistory)
                                UpdateAvatorState(action, out var avator);

                            Foreach<NetworkObject>((t) => t.Init());

                            m_onJoin.ForEach((c) => c.Item1.Invoke());

                            SendWS(new MSG_Join(MSG_Join.MessageType.BROADCAST, @object.avatorAction, null, PhysicsRole.NONE, null).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.BROADCAST:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.BROADCAST));

                            UpdateAvatorState(@object.avatorAction, out var avator);

                            if (userId == 0)
                                Foreach<NetworkObject>((t) => t.SyncViaWebSocket(true, from));

                            m_onJoin.ForEach((c) => c.Item2.Invoke(from));
                        }
                        break;
                }
            });

            RegisterOnMessage(MSG_PhysicsRole.pktId, (from, to, bytes) =>
            {
                SetPhysicsRole(new MSG_PhysicsRole(bytes).physicsRole);
            });

            RegisterOnMessage(MSG_IdAvails.pktId, (from, to, bytes) =>
            {
                var @object = new MSG_IdAvails(bytes);

                switch (@object.messageType)
                {
                    case MSG_IdAvails.MessageType.REQUEST:
                        @object.idAvails = UniqueId.Generate(@object.length);
                        SendWS(@object.Marshall());
                        break;
                    case MSG_IdAvails.MessageType.RESPONSE:
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
                m_adapter.Init(m_adapter.config, @object.room_infos[0].room_id, m_adapter.key, m_adapter.masterKey);

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
            var msgTyp = ToInt32(bytes, Packetable.HEADER_SIZE);

            Debug.Log(THIS_NAME + $"OnMessage: {msgTyp}, Lenght: {bytes.Length}");

            if (m_messageCallbacks.ContainsKey(msgTyp))
                (m_messageCallbacks[msgTyp] as OnMessageCallback).Invoke(from, to, bytes);
        }

        public void OnOpen()
        {
            const string THIS_NAME = "[RTCClient] ";

            m_rtcClient = WebRTCClient.Whep(this, m_adapter, "stream", OnMessage, (() => Debug.Log(THIS_NAME + "OnOpen !"), (_) => { }), (() => Debug.Log(THIS_NAME + "OnClose !"), (_) => { }), () => Debug.LogError(THIS_NAME + "Error !"), new Unity.WebRTC.RTCDataChannelInit(), false, false, null);

            if (userId == 0)
            {
                SetPhysicsRole(PhysicsRole.SEND);

                Foreach<NetworkObject>((t) => t.Init());

                if (m_avatorShop.GetAnchor(0, out var anchor))
                    UpdateAvatorState(PrefabStore.StoreAction.GetInstantiateAction(0, 0, UniqueId.Generate(), anchor), out var avator);
            }
            else
            {
                if (m_avatorShop.GetAnchor(userId, out var anchor))
                    SendWS(0, new MSG_Join(MSG_Join.MessageType.REQUEST, PrefabStore.StoreAction.GetInstantiateAction(0, userId, new Address32(), anchor)).Marshall());
            }
        }

        public void OnClose() => m_onExit.ForEach((c) => c.Item1.Invoke());

        public void OnOpen(int from) => m_onLog.Invoke($"{nameof(OnOpen)}: " + from);

        public void OnClose(int from)
        {
            m_onLog.Invoke($"{nameof(OnClose)}: " + from);

            UpdateAvatorState(PrefabStore.StoreAction.GetDeleteAction(userId), out var avator);

            m_onExit.ForEach((c) => c.Item2.Invoke(from));
        }

        public void OnError()
        {
            throw new NotImplementedException();
        }
    }
}
