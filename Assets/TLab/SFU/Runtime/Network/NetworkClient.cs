using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
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
    public class NetworkClient : MonoBehaviour, ISfuClientEventHandler
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [SerializeField] private Transform m_playerRoot;
        [SerializeField] private Adapter m_adapter;
        [SerializeField] private AvatorConfig m_avatorConfig;
        [SerializeField] private SpawnableStore m_avatorStore;
        [SerializeField] private BaseAnchorProvider m_anchor;
        [SerializeField] private NetworkObjectGroup m_objectGroup;

        [SerializeField] private UnityEvent<string> m_onLog;

        private static WebSocketClient m_wsClient;
        private static WebRTCClient m_rtcClient;

        public static Queue<Address32> idAvails = new Queue<Address32>();

        private static Dictionary<int, SpawnableStore.SpawnAction> m_latestAvatorActions = new Dictionary<int, SpawnableStore.SpawnAction>();

        private Dictionary<string, Coroutine> m_coroutines = new Dictionary<string, Coroutine>();

        private static NetworkClient m_instance;
        public static NetworkClient instance => m_instance;

        private static RigidbodyMode m_rbMode = RigidbodyMode.None;
        public static RigidbodyMode rbMode => m_rbMode;

        public delegate void OnMessageCallback(int from, int to, byte[] bytes);
        private static Hashtable m_messageCallbacks = new Hashtable();

        private static List<(UnityAction, UnityAction<int>)> m_onExit = new List<(UnityAction, UnityAction<int>)>();
        private static List<(UnityAction, UnityAction<int>)> m_onJoin = new List<(UnityAction, UnityAction<int>)>();

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
        public enum RigidbodyMode
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

            public MSG_Join(MessageType messageType, int avatorId) : this(messageType)
            {
                this.messageType = messageType;
                this.avatorId = avatorId;
            }

            public MSG_Join(MessageType messageType, SpawnableStore.SpawnAction avatorAction) : this(messageType)
            {
                this.messageType = messageType;
                this.avatorAction = avatorAction;
            }

            public MSG_Join(MessageType messageType, SpawnableStore.SpawnAction avatorAction, Address32[] idAvails, RigidbodyMode rbMode, SpawnableStore.SpawnAction[] latestAvatorActions, SpawnableStore.SpawnAction[] latestSpawnableActions) : this(messageType)
            {
                this.idAvails = idAvails;
                this.rbMode = rbMode;
                this.avatorAction = avatorAction;
                this.latestAvatorActions = latestAvatorActions;
                this.latestSpawnableActions = latestSpawnableActions;
            }

            public MSG_Join(byte[] bytes) : base(bytes) { }

            public MessageType messageType;

            // Request0
            public int avatorId;

            // Response0, Broadcast
            public SpawnableStore.SpawnAction avatorAction;

            // Response0
            public Address32[] idAvails;
            public RigidbodyMode rbMode;
            public SpawnableStore.SpawnAction[] latestAvatorActions;
            public SpawnableStore.SpawnAction[] latestSpawnableActions;
        }

        [Serializable, Message(typeof(MSG_UpdateRigidbodyMode))]
        public class MSG_UpdateRigidbodyMode : Message
        {
            public MSG_UpdateRigidbodyMode(RigidbodyMode rbMode) : base()
            {
                this.rbMode = rbMode;
            }

            public MSG_UpdateRigidbodyMode(byte[] bytes) : base(bytes) { }

            public RigidbodyMode rbMode;
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

        public static SpawnableStore.SpawnAction[] GetLatestAvatorActionArray() => m_latestAvatorActions.Values.ToArray();
        public static SpawnableStore.SpawnAction[] GetLatestSpawnableActionArray() => SpawnableProvider.GetLatestActionArray();

        public static Dictionary<int, SpawnableStore.SpawnAction> GetLatestAvatorActions() => new Dictionary<int, SpawnableStore.SpawnAction>(m_latestAvatorActions);

        public static void UpdateRigidbodyMode(RigidbodyMode rbMode)
        {
            m_rbMode = rbMode;
            Foreach<NetworkRigidbodyTransform>((t) => t.OnRigidbodyModeChange());
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

        public static void RegisterOnMessage<T>(OnMessageCallback callback) where T : Message => RegisterOnMessage(Message.GetMsgId<T>(), callback);

        public static void UnRegisterOnMessage(int msgId)
        {
            if (m_messageCallbacks.ContainsKey(msgId))
                m_messageCallbacks.Remove(msgId);
        }

        public bool IsPlayerJoined(int index) => m_latestAvatorActions.ContainsKey(index);

        public bool GetLatestAvatorAction(int index, out SpawnableStore.SpawnAction avatorAction)
        {
            if (m_latestAvatorActions.ContainsKey(index))
            {
                avatorAction = m_latestAvatorActions[index];
                return true;
            }
            else
            {
                avatorAction = new SpawnableStore.SpawnAction();
                return false;
            }
        }

        private bool ProcessAvatorAction(SpawnableStore.SpawnAction avatorAction, out SpawnableStore.InstanceRef instanceRef)
        {
            instanceRef = new SpawnableStore.InstanceRef();

            switch (avatorAction.action)
            {
                case SpawnableStore.SpawnAction.Action.Spawn:
                    if (!m_latestAvatorActions.ContainsKey(avatorAction.userId))
                    {
                        var self = avatorAction.userId == userId;
                        if (self)
                        {
                            m_playerRoot.position = avatorAction.transform.position;
                            m_playerRoot.rotation = avatorAction.transform.rotation.ToQuaternion();
                        }

                        if (m_avatorStore.ProcessSpawnAction(avatorAction, out instanceRef))
                        {
                            m_latestAvatorActions.Add(avatorAction.userId, avatorAction);
                            return true;
                        }
                        return false;
                    }
                    return false;
                case SpawnableStore.SpawnAction.Action.DeleteByUserId:
                    if (m_latestAvatorActions.ContainsKey(avatorAction.userId))
                    {
                        m_latestAvatorActions.Remove(avatorAction.userId);

                        m_avatorStore.ProcessSpawnAction(avatorAction, out instanceRef);

                        return true;
                    }
                    return false;
            }
            return false;
        }

        private void OnSyncWorldComplete(SpawnableStore.SpawnAction avatorAction)
        {
            m_onJoin.ForEach((c) => c.Item1.Invoke());

            Debug.Log(THIS_NAME + $"{nameof(OnSyncWorldComplete)}");

            ProcessAvatorAction(avatorAction, out var result);

            SendWS(new MSG_Join(MSG_Join.MessageType.Finish, avatorAction).Marshall());
        }

        private IEnumerator SyncWorldTask(SpawnableStore.SpawnAction avatorAction, NetworkObjectGroup[] groups)
        {
            var complete = false;
            while (!complete)
            {
                yield return new WaitForSeconds(1f);
                complete = true;
                groups.Foreach((t) => {
                    var initialized = (t.state == NetworkObject.State.Initialized) || (t.state == NetworkObject.State.Shutdowned);
                    complete &= initialized;

                    if (!initialized)
                        t.PostSyncRequest();
                });
            }

            OnSyncWorldComplete(avatorAction);

            UnRegisterCoroutine(nameof(SyncWorldTask));

            yield break;
        }

        private void SyncWorldAsync(SpawnableStore.SpawnAction avatorAction, SpawnableStore.SpawnAction[] latestAvatorActions, SpawnableStore.SpawnAction[] latestSpawnableActions)
        {
            var objectGroups = new List<NetworkObjectGroup>() { m_objectGroup };

            m_objectGroup.InitAllObjects(false);

            latestAvatorActions.Foreach((avatorAction) => {
                if (ProcessAvatorAction(avatorAction, out var result))
                {
                    if (result.action == SpawnableStore.SpawnAction.Action.Spawn)
                        objectGroups.Add(result.objectGroup);
                }
            });

            SpawnableProvider.SyncLatestActions(latestSpawnableActions);

            RegisterCoroutine(nameof(SyncWorldTask), StartCoroutine(SyncWorldTask(avatorAction, objectGroups.ToArray())));
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
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Request0) + $":{from}");

                            if (!m_anchor.Get(from, out var anchor))
                                return;

                            var avatorAction = SpawnableStore.SpawnAction.GetRequestSpawnAction(receive.avatorId, from, anchor);

                            var latestAvatorActions = GetLatestAvatorActionArray();
                            var latestSpawnableActions = GetLatestSpawnableActionArray();

                            Debug.Log(THIS_NAME + $"{MSG_Join.MessageType.Request0}");

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.Response0, avatorAction, UniqueNetworkId.Generate(from, 5), RigidbodyMode.Recv, latestAvatorActions, latestSpawnableActions).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.Response0:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Response0) + $":{from}");

                            UniqueNetworkId.AddAvailables(receive.idAvails);

                            UpdateRigidbodyMode(receive.rbMode);

                            var avatorAction = receive.avatorAction;

                            avatorAction.action = SpawnableStore.SpawnAction.Action.Spawn;
                            if (UniqueNetworkId.GetAvailable(out var address)) avatorAction.@public = address;

                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Response0) + $":{from}");

                            SyncWorldAsync(avatorAction, receive.latestAvatorActions, receive.latestSpawnableActions);

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.Request1).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.Request1:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Request1) + $":{from}");

                            SendWS(from, new MSG_Join(MSG_Join.MessageType.Response1).Marshall());
                        }
                        break;
                    case MSG_Join.MessageType.Response1:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Response1) + $":{from}");

                            // Currently, nothing to do ...
                        }
                        break;
                    case MSG_Join.MessageType.Finish:
                        {
                            Debug.Log(THIS_NAME + nameof(MSG_Join.MessageType.Finish) + $":{JsonUtility.ToJson(receive.avatorAction)}");

                            ProcessAvatorAction(receive.avatorAction, out var result);

                            m_onJoin.ForEach((c) => c.Item2.Invoke(from));
                        }
                        break;
                }
            });

            RegisterOnMessage<MSG_UpdateRigidbodyMode>((from, to, bytes) =>
            {
                UpdateRigidbodyMode(new MSG_UpdateRigidbodyMode(bytes).rbMode);
            });

            RegisterOnMessage<MSG_IdAvails>((from, to, bytes) =>
            {
                var receive = new MSG_IdAvails(bytes);

                switch (receive.messageType)
                {
                    case MSG_IdAvails.MessageType.Request:
                        receive.idAvails = UniqueNetworkId.Generate(from, receive.length);
                        SendWS(receive.Marshall());
                        break;
                    case MSG_IdAvails.MessageType.Response:
                        UniqueNetworkId.AddAvailables(receive.idAvails);
                        break;
                }
            });
            #endregion ADD_CALLBACKS

            m_wsClient = WebSocketClient.Open(this, m_adapter, "master", OnMessage, (OnOpen, OnOpen), (OnClose, OnClose), OnError);

            UnRegisterCoroutine(nameof(ConnectTask));

            yield break;
        }

        private void RegisterCoroutine(string key, Coroutine coroutine)
        {
            if (m_coroutines.ContainsKey(key))
            {
                var tmp = m_coroutines[key];
                if (tmp != null)
                    StopCoroutine(tmp);
            }
            m_coroutines[key] = coroutine;
        }

        private void UnRegisterCoroutine(string key)
        {
            if (m_coroutines.ContainsKey(key))
            {
                var coroutine = m_coroutines[key];
                if (coroutine != null)
                    StopCoroutine(coroutine);
                m_coroutines.Remove(key);
            }
        }

        public static void ConnectAsync() => instance.RegisterCoroutine(nameof(ConnectTask), instance.StartCoroutine(instance.ConnectTask()));

        #region WS

        public static Task SendWS(int to, byte[] bytes)
        {
            if (wsEnabled)
                return m_wsClient.Send(to, bytes);

            return new Task(() => { });
        }

        public static Task SendWS(int to, string message) => SendWS(to, Encoding.UTF8.GetBytes(message));

        public static Task SendWS(byte[] bytes) => SendWS(userId, bytes);

        public static async void CloseWS()
        {
            if (m_wsClient != null)
                await m_wsClient.HangUp();

            m_wsClient = null;
        }

        #endregion WS

        #region RTC

        public static void SendRTC(int to, byte[] bytes) => m_rtcClient?.Send(to, bytes);

        public static void SendRTC(byte[] bytes) => SendRTC(userId, bytes);

        public static void SendRTC(int to, string text) => m_rtcClient?.Send(to, text);

        public static void SendRTC(string text) => SendRTC(userId, text);

        public static void CloseRTC()
        {
            m_rtcClient?.HangUp();
            m_rtcClient = null;
        }

        #endregion RTC

        private void Awake()
        {
            if (m_instance != this)
            {
                m_instance = this;
                DontDestroyOnLoad(this);
            }
        }

        public static void HangUpAll()
        {
            CloseRTC();
            CloseWS();
        }

        private void OnDestroy() => HangUpAll();

        private void OnApplicationQuit() => HangUpAll();

        public void OnMessage(int from, int to, byte[] bytes)
        {
            var msgId = ToInt32(bytes, SfuClient.RECV_PACKET_HEADER_SIZE);

            if (m_messageCallbacks.ContainsKey(msgId))
                (m_messageCallbacks[msgId] as OnMessageCallback).Invoke(from, to, bytes);
            else
                Debug.LogError(THIS_NAME + $"{nameof(OnMessage)}:{msgId} not found");
        }

        public void OnOpen()
        {
            const string THIS_NAME = "[RTCClient] ";

            m_rtcClient = WebRTCClient.Whep(this, m_adapter, "stream", OnMessage, (() => Debug.Log(THIS_NAME + "OnOpen !"), (_) => { }), (() => Debug.Log(THIS_NAME + "OnClose !"), (_) => { }), () => Debug.LogError(THIS_NAME + "Error !"), new Unity.WebRTC.RTCDataChannelInit(), false, false, null);

            if (userId == 0)
            {
                UpdateRigidbodyMode(RigidbodyMode.Send);

                m_objectGroup.InitAllObjects(true);

                UniqueNetworkId.AddAvailables(UniqueNetworkId.Generate(0, 5));

                if (m_anchor.Get(0, out var anchor))
                {
                    var address = UniqueNetworkId.Generate(0);
                    ProcessAvatorAction(SpawnableStore.SpawnAction.GetSpawnAction(m_avatorConfig.avatorId, address, anchor), out var result);
                }
            }
            else
                SendWS(0, new MSG_Join(MSG_Join.MessageType.Request0, m_avatorConfig.avatorId).Marshall());
        }

        public void OnClose() => m_onExit.ForEach((c) => c.Item1.Invoke());

        public void OnOpen(int from) => m_onLog.Invoke($"{nameof(OnOpen)}: " + from);

        public void OnClose(int from)
        {
            m_onLog.Invoke($"{nameof(OnClose)}: " + from);

            ProcessAvatorAction(SpawnableStore.SpawnAction.GetDeleteAction(from), out var result);

            UniqueNetworkId.OnUserExit(from);

            m_onExit.ForEach((c) => c.Item2.Invoke(from));
        }

        public void OnError()
        {
            throw new NotImplementedException();
        }
    }
}
