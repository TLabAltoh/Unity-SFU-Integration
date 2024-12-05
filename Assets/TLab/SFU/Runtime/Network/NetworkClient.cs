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
    using PrefabShopRegistry = Registry<string, PrefabShop>;

    [AddComponentMenu("TLab/SFU/Network Client (TLab)")]
    public class NetworkClient : MonoBehaviour, INetworkConnectionEventHandler
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [SerializeField] private Adapter m_adapter;
        [SerializeField] private PrefabStore m_avatorStore;
        [SerializeField] private BaseAnchorProvider m_anchor;

        [SerializeField] private UnityEvent<string> m_onLog;

        private static WebSocketClient m_wsClient;
        private static WebRTCClient m_rtcClient;

        public static Queue<Address32> idAvails = new Queue<Address32>();

        private Dictionary<int, PrefabStore.StoreAction> m_latestAvatorActions = new Dictionary<int, PrefabStore.StoreAction>();
        public PrefabStore.StoreAction[] latestAvatorActions => m_latestAvatorActions.Values.ToArray();

        private IEnumerator m_connectTask = null;
        private IEnumerator m_waitForInitTask = null;

        public static NetworkClient instance;

        private static PhysicsRole m_physicsRole = PhysicsRole.None;
        public static PhysicsRole physicsRole => m_physicsRole;

        public delegate void OnMessageCallback(int from, int to, byte[] bytes);
        private static Hashtable m_messageCallbacks = new Hashtable();

        private static List<(UnityAction, UnityAction<int>)> m_onJoin = new List<(UnityAction, UnityAction<int>)>();
        private static List<(UnityAction, UnityAction<int>)> m_onExit = new List<(UnityAction, UnityAction<int>)>();

        public const int MESSAGE_HEADER_SIZE = 4;   // msgId (4)

        public const int PAYLOAD_OFFSET = SfuClient.RECV_PACKET_HEADER_SIZE + MESSAGE_HEADER_SIZE;

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
            None,
            Send,
            Recv,
        };

        public static class DstIndex
        {
            public const int BROADCAST = -1;
        }

        #endregion STRUCT

        #region MESSAGE

        [Serializable, Message(typeof(MSG_Join))]
        public class MSG_Join : Message
        {
            public enum MessageType
            {
                Request0 = 0,
                Response0 = 1,
                Request1 = 2,
                Response1 = 3,
                Finish = 4,
            };

            public MSG_Join(MessageType messageType) : base()
            {
                this.messageType = messageType;
            }

            public MSG_Join(MessageType messageType, PrefabStore.StoreAction avatorAction) : this(messageType)
            {
                this.messageType = messageType;
                this.avatorAction = avatorAction;
            }

            public MSG_Join(MessageType messageType, PrefabStore.StoreAction avatorAction, Address32[] idAvails, PhysicsRole physicsRole, PrefabShop.State[] latestShopStates, PrefabStore.StoreAction[] latestAvatorActions) : this(messageType, avatorAction)
            {
                this.idAvails = idAvails;
                this.physicsRole = physicsRole;
                this.latestShopStates = latestShopStates;
                this.latestAvatorActions = latestAvatorActions;
            }

            public MSG_Join(byte[] bytes) : base(bytes) { }

            public MessageType messageType;

            // Request0, Response0
            public PrefabStore.StoreAction avatorAction;

            // Response0
            public Address32[] idAvails;
            public PhysicsRole physicsRole;
            public PrefabShop.State[] latestShopStates;
            public PrefabStore.StoreAction[] latestAvatorActions;
        }

        [Serializable, Message(typeof(MSG_PhysicsRole))]
        public class MSG_PhysicsRole : Message
        {
            public MSG_PhysicsRole(PhysicsRole physicsRole) : base()
            {
                this.physicsRole = physicsRole;
            }

            public MSG_PhysicsRole(byte[] bytes) : base(bytes) { }

            public PhysicsRole physicsRole;
        }

        [Serializable, Message(typeof(MSG_IdAvails))]
        public class MSG_IdAvails : Message
        {
            public MSG_IdAvails(MessageType messageType, int length, Address32[] idAvails) : base()
            {
                this.messageType = messageType;
                this.length = length;
                this.idAvails = idAvails;
            }

            public MSG_IdAvails(byte[] bytes) : base(bytes) { }

            public enum MessageType
            {
                Request = 0,
                Response = 1,
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
            if (!m_onJoin.Contains(@object)) m_onJoin.Add(@object);
        }

        public static void UnRegisterOnJoin(UnityAction callback0, UnityAction<int> callback1)
        {
            var @object = (callback0, callback1);
            if (m_onJoin.Contains(@object)) m_onJoin.Remove(@object);
        }

        public static void RegisterOnExit(UnityAction callback0, UnityAction<int> callback1)
        {
            var @object = (callback0, callback1);
            if (!m_onExit.Contains(@object)) m_onExit.Add(@object);
        }

        public static void UnRegisterOnExit(UnityAction callback0, UnityAction<int> callback1)
        {
            var @object = (callback0, callback1);
            if (m_onExit.Contains(@object)) m_onExit.Remove(@object);
        }

        public static void RegisterOnMessage(int msgId, OnMessageCallback callback)
        {
            if (!m_messageCallbacks.ContainsKey(msgId))
                m_messageCallbacks[msgId] = callback;
        }

        public static void RegisterOnMessage<T>(OnMessageCallback callback) where T : Message
        {
            RegisterOnMessage(Message.GetMsgId<T>(), callback);
        }

        public static void UnRegisterOnMessage(int msgId)
        {
            if (m_messageCallbacks.ContainsKey(msgId))
                m_messageCallbacks.Remove(msgId);
        }

        public bool IsPlayerJoined(int index) => m_latestAvatorActions.ContainsKey(index);

        public bool GetLatestAvatorAction(int index, out PrefabStore.StoreAction avatorAction)
        {
            if (m_latestAvatorActions.ContainsKey(index))
            {
                avatorAction = m_latestAvatorActions[index];
                return true;
            }
            else
            {
                avatorAction = new PrefabStore.StoreAction();
                return false;
            }
        }

        private bool ProcessAvatorAction(PrefabStore.StoreAction avatorAction, out PrefabStore.Result result)
        {
            result = new PrefabStore.Result();

            switch (avatorAction.action)
            {
                case PrefabStore.StoreAction.Action.Spawn:
                    if (!m_latestAvatorActions.ContainsKey(avatorAction.userId))
                    {
                        m_latestAvatorActions.Add(avatorAction.userId, avatorAction);

                        m_avatorStore.ProcessStoreAction(avatorAction, out result);

                        return true;
                    }
                    return false;
                case PrefabStore.StoreAction.Action.DeleteByUserId:
                    if (m_latestAvatorActions.ContainsKey(avatorAction.userId))
                    {
                        m_latestAvatorActions.Remove(avatorAction.userId);

                        m_avatorStore.ProcessStoreAction(avatorAction, out result);

                        return true;
                    }
                    return false;
            }
            return false;
        }

        private void SyncPrefabShopState(PrefabShop.State shopState) => PrefabShopRegistry.GetByKey(shopState.storeId)?.SyncState(shopState);

        private IEnumerator WaitForInitTask(PrefabStore.StoreAction avatorAction)
        {
            var initialized = false;
            while (!initialized)
            {
                Foreach<NetworkObject>((t) => initialized &= (t.state == NetworkObject.State.Initialized));
                yield return new WaitForSeconds(0.1f);
            }
            m_waitForInitTask = null;

            m_onJoin.ForEach((c) => c.Item1.Invoke());

            SendWS(new MSG_Join(MSG_Join.MessageType.Finish, avatorAction).Marshall());

            yield break;
        }

        private IEnumerator ConnectTask()
        {
            yield return null;

            CloseWS();

            yield return null;

            #region ADD_CALLBACKS

            RegisterOnMessage<MSG_Join>((from, to, bytes) =>
            {
                var @object = new MSG_Join(bytes);

                switch (@object.messageType)
                {
                    case MSG_Join.MessageType.Request0:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Request0));

                            var avatorAction = @object.avatorAction;
                            avatorAction.publicId = UniqueId.Generate();

                            var latestShopStates = PrefabShopRegistry.values.Select((t) => t.GetState()).ToArray();

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.Response0, avatorAction, UniqueId.Generate(5), PhysicsRole.Recv, latestShopStates, latestAvatorActions).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.Response0:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Response0));

                            SetPhysicsRole(@object.physicsRole);

                            @object.latestAvatorActions.Foreach((avatorAction) => ProcessAvatorAction(avatorAction, out var result));

                            Foreach<NetworkObject>((t) => t.Init(false));

                            @object.latestShopStates.Foreach((shopState) => SyncPrefabShopState(shopState));

                            m_waitForInitTask = WaitForInitTask(@object.avatorAction);

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.Request1).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.Request1:
                        {
                            Foreach<NetworkObject>((t) => t.SyncViaWebSocket(true, from));
                        }
                        break;
                    case MSG_Join.MessageType.Response1:
                        {
                            // Currently, nothing to do ...
                        }
                        break;
                    case MSG_Join.MessageType.Finish:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Finish));

                            ProcessAvatorAction(@object.avatorAction, out var result);

                            m_onJoin.ForEach((c) => c.Item2.Invoke(from));
                        }
                        break;
                }
            });

            RegisterOnMessage<MSG_PhysicsRole>((from, to, bytes) =>
            {
                SetPhysicsRole(new MSG_PhysicsRole(bytes).physicsRole);
            });

            RegisterOnMessage<MSG_IdAvails>((from, to, bytes) =>
            {
                var @object = new MSG_IdAvails(bytes);

                switch (@object.messageType)
                {
                    case MSG_IdAvails.MessageType.Request:
                        @object.idAvails = UniqueId.Generate(@object.length);
                        SendWS(@object.Marshall());
                        break;
                    case MSG_IdAvails.MessageType.Response:
                        break;
                }
            });
            #endregion ADD_CALLBACKS

            m_wsClient = WebSocketClient.Open(this, m_adapter, "master", OnMessage, (OnOpen, OnOpen), (OnClose, OnClose), OnError);

            m_connectTask = null;

            yield break;
        }

        private void OnJoin(string @string)
        {
            m_onLog.Invoke(@string);
            m_connectTask = ConnectTask();
        }

        private void OnCreate(string @string)
        {
            m_onLog.Invoke(@string);
            m_adapter.Join(this, OnJoin);
        }

        public void Join() => m_adapter.GetInfo(this, (@string) =>
        {
            m_onLog.Invoke(@string);
            var @object = JsonUtility.FromJson<Answer.Infos>(@string);

            if (@object.room_infos.Length == 0)
                m_adapter.Create(this, OnCreate);
            else
            {
                m_onLog.Invoke(@string);
                m_adapter.Init(m_adapter.config, @object.room_infos[0].room_id, m_adapter.key, m_adapter.masterKey);
                m_adapter.Join(this, OnJoin);
            }
        });

        private void OnExit(string @string)
        {
            m_onLog.Invoke(@string);
            m_onExit.ForEach((c) => c.Item1.Invoke());
        }

        public void Exit()
        {
            m_rtcClient?.HangUp();
            m_adapter.Exit(this, OnExit);
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

        private void Update()
        {
            m_connectTask?.MoveNext();
            m_waitForInitTask?.MoveNext();
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
            var msgId = ToInt32(bytes, SfuClient.RECV_PACKET_HEADER_SIZE);

            if (m_messageCallbacks.ContainsKey(msgId))
                (m_messageCallbacks[msgId] as OnMessageCallback).Invoke(from, to, bytes);
        }

        public void OnOpen()
        {
            const string THIS_NAME = "[RTCClient] ";

            m_rtcClient = WebRTCClient.Whep(this, m_adapter, "stream", OnMessage, (() => Debug.Log(THIS_NAME + "OnOpen !"), (_) => { }), (() => Debug.Log(THIS_NAME + "OnClose !"), (_) => { }), () => Debug.LogError(THIS_NAME + "Error !"), new Unity.WebRTC.RTCDataChannelInit(), false, false, null);

            if (userId == 0)
            {
                SetPhysicsRole(PhysicsRole.Send);

                Foreach<NetworkObject>((t) => t.Init(true));

                if (m_anchor.Get(0, out var anchor))
                    ProcessAvatorAction(PrefabStore.StoreAction.GetSpawnAction(0, 0, UniqueId.Generate(), anchor), out var result);
            }
            else
            {
                if (m_anchor.Get(userId, out var anchor))
                    SendWS(0, new MSG_Join(MSG_Join.MessageType.Request0, PrefabStore.StoreAction.GetSpawnAction(0, userId, new Address32(), anchor)).Marshall());
            }
        }

        public void OnClose() => m_onExit.ForEach((c) => c.Item1.Invoke());

        public void OnOpen(int from) => m_onLog.Invoke($"{nameof(OnOpen)}: " + from);

        public void OnClose(int from)
        {
            m_onLog.Invoke($"{nameof(OnClose)}: " + from);

            ProcessAvatorAction(PrefabStore.StoreAction.GetDeleteAction(userId), out var result);

            m_onExit.ForEach((c) => c.Item2.Invoke(from));
        }

        public void OnError()
        {
            throw new NotImplementedException();
        }
    }
}
