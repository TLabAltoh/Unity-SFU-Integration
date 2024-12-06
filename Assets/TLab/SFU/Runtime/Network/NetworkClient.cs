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
    using SpawnableShopRegistry = Registry<string, SpawnableShop>;

    [AddComponentMenu("TLab/SFU/Network Client (TLab)")]
    public class NetworkClient : MonoBehaviour, INetworkConnectionEventHandler
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [SerializeField] private Adapter m_adapter;
        [SerializeField] private SpawnableStore m_avatorStore;
        [SerializeField] private BaseAnchorProvider m_anchor;
        [SerializeField] private NetworkObjectGroup m_objectGroup;

        [SerializeField] private UnityEvent<string> m_onLog;

        private static WebSocketClient m_wsClient;
        private static WebRTCClient m_rtcClient;

        public static Queue<Address32> idAvails = new Queue<Address32>();

        private Dictionary<int, SpawnableStore.StoreAction> m_latestAvatorActions = new Dictionary<int, SpawnableStore.StoreAction>();
        public SpawnableStore.StoreAction[] latestAvatorActions => m_latestAvatorActions.Values.ToArray();

        private IEnumerator m_connectTask = null;
        private IEnumerator m_syncWorldTask = null;

        public static NetworkClient instance;

        private static PhysicsBehaviour m_physicsBehaviour = PhysicsBehaviour.None;
        public static PhysicsBehaviour physicsBehaviour => m_physicsBehaviour;

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
        public enum PhysicsBehaviour
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

            public MSG_Join(MessageType messageType, SpawnableStore.StoreAction avatorAction) : this(messageType)
            {
                this.messageType = messageType;
                this.avatorAction = avatorAction;
            }

            public MSG_Join(MessageType messageType, SpawnableStore.StoreAction avatorAction, Address32[] idAvails, PhysicsBehaviour physicsBehaviour, SpawnableShop.State[] latestShopStates, SpawnableStore.StoreAction[] latestAvatorActions) : this(messageType, avatorAction)
            {
                this.idAvails = idAvails;
                this.physicsBehaviour = physicsBehaviour;
                this.latestShopStates = latestShopStates;
                this.latestAvatorActions = latestAvatorActions;
            }

            public MSG_Join(byte[] bytes) : base(bytes) { }

            public MessageType messageType;

            // Request0, Response0
            public SpawnableStore.StoreAction avatorAction;

            // Response0
            public Address32[] idAvails;
            public PhysicsBehaviour physicsBehaviour;
            public SpawnableShop.State[] latestShopStates;
            public SpawnableStore.StoreAction[] latestAvatorActions;
        }

        [Serializable, Message(typeof(MSG_UpdatePhysicsBehaviour))]
        public class MSG_UpdatePhysicsBehaviour : Message
        {
            public MSG_UpdatePhysicsBehaviour(PhysicsBehaviour physicsBehaviour) : base()
            {
                this.physicsBehaviour = physicsBehaviour;
            }

            public MSG_UpdatePhysicsBehaviour(byte[] bytes) : base(bytes) { }

            public PhysicsBehaviour physicsBehaviour;
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

        public void UpdatePhysicsBehaviour(PhysicsBehaviour physicsBehaviour)
        {
            m_physicsBehaviour = physicsBehaviour;
            Foreach<NetworkTransform>((t) => t.OnPhysicsBehaviourChange());
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

        public bool GetLatestAvatorAction(int index, out SpawnableStore.StoreAction avatorAction)
        {
            if (m_latestAvatorActions.ContainsKey(index))
            {
                avatorAction = m_latestAvatorActions[index];
                return true;
            }
            else
            {
                avatorAction = new SpawnableStore.StoreAction();
                return false;
            }
        }

        private bool ProcessAvatorAction(SpawnableStore.StoreAction avatorAction, out SpawnableStore.Result result)
        {
            result = new SpawnableStore.Result();

            switch (avatorAction.action)
            {
                case SpawnableStore.StoreAction.Action.Spawn:
                    if (!m_latestAvatorActions.ContainsKey(avatorAction.userId))
                    {
                        m_latestAvatorActions.Add(avatorAction.userId, avatorAction);

                        m_avatorStore.ProcessStoreAction(avatorAction, out result);

                        return true;
                    }
                    return false;
                case SpawnableStore.StoreAction.Action.DeleteByUserId:
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

        private void SyncSpawnableShopState(SpawnableShop.State shopState) => SpawnableShopRegistry.GetByKey(shopState.storeId)?.SyncState(shopState);

        private void OnSyncWorldComplete(SpawnableStore.StoreAction avatorAction)
        {
            m_onJoin.ForEach((c) => c.Item1.Invoke());

            Debug.Log(THIS_NAME + $"{nameof(SyncWorldTask)}");

            SendWS(new MSG_Join(MSG_Join.MessageType.Finish, avatorAction).Marshall());
        }

        private IEnumerator SyncWorldTask(SpawnableStore.StoreAction avatorAction, NetworkObjectGroup[] groups)
        {
            var complete = false;
            while (!complete)
            {
                groups.Foreach((t) => complete &= (t.state == NetworkObject.State.Initialized));
                yield return null;
            }
            m_syncWorldTask = null;

            OnSyncWorldComplete(avatorAction);

            yield break;
        }

        private void SyncWorldAsync(SpawnableStore.StoreAction avatorAction, SpawnableStore.StoreAction[] latestAvatorActions, SpawnableShop.State[] latestShopStates)
        {
            var objectGroups = new List<NetworkObjectGroup>() { m_objectGroup };

            m_objectGroup.InitAllObjects(false);

            latestAvatorActions.Foreach((avatorAction) => {
                if (ProcessAvatorAction(avatorAction, out var result))
                {
                    if (result.action == SpawnableStore.StoreAction.Action.Spawn)
                        objectGroups.Add(result.objectGroup);
                }
            });

            latestShopStates.Foreach((shopState) => SyncSpawnableShopState(shopState));

            m_syncWorldTask = SyncWorldTask(avatorAction, objectGroups.ToArray());
        }

        private IEnumerator ConnectTask()
        {
            yield return null;

            CloseWS();

            yield return null;

            #region ADD_CALLBACKS

            RegisterOnMessage<MSG_Join>((from, to, bytes) =>
            {
                var receive = new MSG_Join(bytes);

                switch (receive.messageType)
                {
                    case MSG_Join.MessageType.Request0:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Request0));

                            var avatorAction = receive.avatorAction;
                            avatorAction.publicId = UniqueId.Generate();

                            var latestShopStates = SpawnableShopRegistry.values.Select((t) => t.GetState()).ToArray();

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.Response0, avatorAction, UniqueId.Generate(5), PhysicsBehaviour.Recv, latestShopStates, latestAvatorActions).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.Response0:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Response0));

                            UpdatePhysicsBehaviour(receive.physicsBehaviour);

                            SyncWorldAsync(receive.avatorAction, receive.latestAvatorActions, receive.latestShopStates);

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.Request1).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.Request1:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Request1));

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.Response1).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.Response1:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Response1));

                            // Currently, nothing to do ...
                        }
                        break;
                    case MSG_Join.MessageType.Finish:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Finish));

                            ProcessAvatorAction(receive.avatorAction, out var result);

                            m_onJoin.ForEach((c) => c.Item2.Invoke(from));
                        }
                        break;
                }
            });

            RegisterOnMessage<MSG_UpdatePhysicsBehaviour>((from, to, bytes) =>
            {
                UpdatePhysicsBehaviour(new MSG_UpdatePhysicsBehaviour(bytes).physicsBehaviour);
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
            m_syncWorldTask?.MoveNext();
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
                UpdatePhysicsBehaviour(PhysicsBehaviour.Send);

                Foreach<NetworkObject>((t) => t.Init(true));

                if (m_anchor.Get(0, out var anchor))
                    ProcessAvatorAction(SpawnableStore.StoreAction.GetSpawnAction(0, 0, UniqueId.Generate(), anchor), out var result);
            }
            else
            {
                if (m_anchor.Get(userId, out var anchor))
                    SendWS(0, new MSG_Join(MSG_Join.MessageType.Request0, SpawnableStore.StoreAction.GetSpawnAction(0, userId, new Address32(), anchor)).Marshall());
            }
        }

        public void OnClose() => m_onExit.ForEach((c) => c.Item1.Invoke());

        public void OnOpen(int from) => m_onLog.Invoke($"{nameof(OnOpen)}: " + from);

        public void OnClose(int from)
        {
            m_onLog.Invoke($"{nameof(OnClose)}: " + from);

            ProcessAvatorAction(SpawnableStore.StoreAction.GetDeleteAction(userId), out var result);

            m_onExit.ForEach((c) => c.Item2.Invoke(from));
        }

        public void OnError()
        {
            throw new NotImplementedException();
        }
    }
}
