using System.Threading.Tasks;
using System.Collections;
using System;
using System.Text;
using UnityEngine;
using static TLab.SFU.ComponentExtention;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Sync Client (TLab)")]
    public class SyncClient : MonoBehaviour
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [SerializeField] private Adapter m_adapter;
        [SerializeField] private PrefabStore m_prefabStore;

        [Header("Anchors")]
        [SerializeField] private Transform[] m_instantiateAnchors;
        [SerializeField] private Transform[] m_respownAnchors;

        private static WebSocketClient m_masterChannel;
        private static WebRTCClient m_rtcChannel;

        private static PhysicsUpdateType m_physicsUpdateType = PhysicsUpdateType.NONE;

        public static PhysicsUpdateType physicsUpdateType => m_physicsUpdateType;

        public delegate void MasterChannelCallback(int from, byte[] bytes);
        private static Hashtable m_masterChannelCallbacks = new Hashtable();

        private IEnumerator m_connectMasterChannelTask = null;

        public static SyncClient instance;

        public const int HEADER_SIZE = 4;   // pktId (4)

        public const int PAYLOAD_OFFSET = SfuClient.PACKET_HEADER_SIZE + HEADER_SIZE;

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

        public static bool masterChannelConnected
        {
            get
            {
                if (m_masterChannel == null)
                {
                    return false;
                }

                return m_masterChannel.connected;
            }
        }

        public static bool masterChannelEnabled
        {
            get
            {
                if (userAdapter == null)
                {
                    return false;
                }

                return userAdapter.regested && masterChannelConnected;
            }
        }

        public static bool rtcChannelConnected
        {
            get
            {
                if (m_rtcChannel == null)
                {
                    return false;
                }

                return true;  // TODO: + Connected
            }
        }

        public static bool rtcChannelEnabled
        {
            get
            {
                if (userAdapter == null)
                {
                    return false;
                }

                return userAdapter.regested && rtcChannelConnected;
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

        #region MESSAGE_TYPE

        [Serializable]
        public struct MCH_Join : Packetable
        {
            public static int pktId;

            static MCH_Join() => pktId = nameof(MCH_Join).GetHashCode();

            public int messageType; // 0: request, 1: response, 2: broadcast
            public PrefabStore.StoreAction avatorInstantiateInfo;

            // response
            public Address32[] idAvails;
            public PhysicsUpdateType physicsUpdateType;
            public PrefabStore.StoreAction[] othersInstantiateInfos;

            public byte[] Marshall()
            {
                var json = JsonUtility.ToJson(this);
                return UnsafeUtility.Combine(pktId, Encoding.UTF8.GetBytes(json));
            }

            public void UnMarshall(byte[] bytes)
            {
                var json = Encoding.UTF8.GetString(bytes, PAYLOAD_OFFSET, bytes.Length - PAYLOAD_OFFSET);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }

        [Serializable]
        public struct MCH_PhysicsUpdateType : Packetable
        {
            public static int pktId;

            static MCH_PhysicsUpdateType() => pktId = nameof(MCH_PhysicsUpdateType).GetHashCode();

            public PhysicsUpdateType physicsUpdateType;

            public byte[] Marshall()
            {
                var json = JsonUtility.ToJson(this);
                return UnsafeUtility.Combine(pktId, Encoding.UTF8.GetBytes(json));
            }

            public void UnMarshall(byte[] bytes)
            {
                var json = Encoding.UTF8.GetString(bytes, PAYLOAD_OFFSET, bytes.Length - PAYLOAD_OFFSET);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }

        public struct MCH_IdAvails : Packetable
        {
            public static int pktId;

            static MCH_IdAvails() => pktId = nameof(MCH_IdAvails).GetHashCode();

            public int messageType; // 0: request, 1: response
            public int length;
            public Address32[] idAvails;

            public byte[] Marshall()
            {
                var json = JsonUtility.ToJson(this);
                return UnsafeUtility.Combine(pktId, Encoding.UTF8.GetBytes(json));
            }

            public void UnMarshall(byte[] bytes)
            {
                var json = Encoding.UTF8.GetString(bytes, PAYLOAD_OFFSET, bytes.Length - PAYLOAD_OFFSET);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }

        #endregion MESSAGE_TYPE

        #region REFLESH

        //public void ForceReflesh(bool reloadWorldData)
        //{
        //    MasterChannelSend(action: WebAction.REFLESH, active: reloadWorldData);
        //}

        //public void UniReflesh(string id)
        //{
        //    MasterChannelSend(action: WebAction.UNI_REFLESH_TRANSFORM, transform: new WebObjectInfo { id = id });
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

        #region MASTER_CHANNEL

        public static void RegisterMasterChannelCallback(int msgId, MasterChannelCallback callback)
        {
            if (!m_masterChannelCallbacks.ContainsKey(msgId))
            {
                m_masterChannelCallbacks[msgId] = callback;
            }
        }

        private IEnumerator ConnectMasterChannelTask()
        {
            yield return null;

            CloseMasterChannel();

            yield return null;

            #region ADD_CALLBACKS

            RegisterMasterChannelCallback(MCH_Join.pktId, (from, bytes) =>
            {
                var @object = new MCH_Join();
                @object.UnMarshall(bytes);

                switch (@object.messageType)
                {
                    case 0: // request
                        {
                            var response = new MCH_Join();
                            response.messageType = 1;
                            response.avatorInstantiateInfo = @object.avatorInstantiateInfo;

                            // response
                            response.avatorInstantiateInfo.publicId = UniqueId.Generate();
                            response.idAvails = UniqueId.Generate(5);   // TODO
                            response.physicsUpdateType = PhysicsUpdateType.RECEIVER;
                            response.othersInstantiateInfos = roomAdapter.avatorInstantiateHistorys;

                            roomAdapter.UpdateState(@object.avatorInstantiateInfo, out var avator);

                            MasterChannelSend(response.avatorInstantiateInfo.userId, response.Marshall());

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

            RegisterMasterChannelCallback(MCH_PhysicsUpdateType.pktId, (from, bytes) =>
            {
                var @object = new MCH_Join();
                @object.UnMarshall(bytes);

                UpdatePhysicsUpdateType(@object.physicsUpdateType);
            });

            RegisterMasterChannelCallback(MCH_IdAvails.pktId, (from, bytes) =>
            {
                var @object = new MCH_Join();
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
            m_masterChannelCallbacks[(int)WebAction.ACEPT] = (from, obj) => {

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

                m_rtcChannel.Join(GetClientID(m_id), m_roomConfig.id);

            };
            m_masterChannelCallbacks[(int)WebAction.GUEST_DISCONNECT] = (from, obj) => {

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
            m_masterChannelCallbacks[(int)WebAction.GUEST_PARTICIPATION] = (from, obj) => {

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

            m_masterChannel = WebSocketClient.Open(this, m_adapter, "master", OnReceive, OnConnect, OnDisconnect);

            m_connectMasterChannelTask = null;

            yield return 1;
        }

        private void OnReceive(int from, int to, byte[] bytes)
        {
            var msgTyp = bytes[0];

            if (m_masterChannelCallbacks.ContainsKey(msgTyp))
            {
                var callback = m_masterChannelCallbacks[msgTyp] as MasterChannelCallback;
                callback.Invoke(from, bytes);
            }
        }

        private void OnConnect(int from)
        {

        }

        private void OnDisconnect(int from)
        {

        }

        public void ConnectMasterChannel()
        {
            m_connectMasterChannelTask = ConnectMasterChannelTask();
        }

        public Task MasterChannelSend(int to, byte[] bytes)
        {
            if (masterChannelEnabled)
                return m_masterChannel.Send(to, bytes);

            return new Task(() => { });
        }

        public Task MasterChannelSend(int to, string message) => MasterChannelSend(to, Encoding.UTF8.GetBytes(message));

        public Task MasterChannelSend(byte[] bytes) => MasterChannelSend(userAdapter.id, bytes);

        public async void CloseMasterChannel()
        {
            await m_masterChannel?.HangUp();
            m_masterChannel = null;
        }

        #endregion MASTER_CHANNEL

        #region RTC_CHANNEL

        public unsafe void OnReceiveRTC(int to, int from, byte[] bytes)
        {
            int headerLen = 8, payloadLen = bytes.Length - headerLen;
            var targetId = new Address64();

            fixed (byte* bytesPtr = bytes)
                targetId.Copy(bytesPtr);

            var networkedObject = NetworkedObject.GetById(targetId);
            if (networkedObject == null)
            {
                Debug.LogError($"Networked object not found: {targetId}");
                return;
            }

            var payload = new byte[payloadLen];
            Array.Copy(bytes, headerLen, payload, 0, payloadLen);

            networkedObject.OnReceive(to, from, payload);
        }

        public void SendRTC(int to, byte[] bytes) => m_rtcChannel?.Send(to, bytes);

        public void SendRTC(byte[] bytes) => SendRTC(userAdapter.id, bytes);

        public void SendTextRTC(int to, string text) => m_rtcChannel?.SendText(to, text);

        public void SendTextRTC(string text) => SendTextRTC(userAdapter.id, text);

        public void CloseRTCChannel()
        {
            m_rtcChannel?.HangUp();
            m_rtcChannel = null;
        }

        #endregion RTC_CHANNEL

        void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            ConnectMasterChannel();
        }

        private void Update()
        {
            m_connectMasterChannelTask?.MoveNext();
        }

        private void OnDestroy()
        {
            CloseRTCChannel();
            CloseMasterChannel();
        }

        private void OnApplicationQuit()
        {
            CloseRTCChannel();
            CloseMasterChannel();
        }
    }
}
