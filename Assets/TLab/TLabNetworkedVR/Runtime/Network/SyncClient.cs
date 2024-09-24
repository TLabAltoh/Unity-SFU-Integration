using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using NativeWebSocket;
using TLab.NetworkedVR.Network.WebRTC;
using static TLab.NetworkedVR.ComponentExtention;

namespace TLab.NetworkedVR.Network
{
    [AddComponentMenu("TLab/NetworkedVR/Sync Client (TLab)")]
    public class SyncClient : MonoBehaviour
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [SerializeField] private Adapter m_adapter;
        [SerializeField] private PrefabStore m_prefabStore;

        [Header("Anchors")]
        [SerializeField] private Transform[] m_instantiateAnchors;
        [SerializeField] private Transform[] m_respownAnchors;

        private static WebSocket m_masterChannel;

        private static WebRTCClient m_rtcChannel;

        private static PhysicsUpdateType m_physicsUpdateType = PhysicsUpdateType.NONE;

        public static PhysicsUpdateType physicsUpdateType => m_physicsUpdateType;

        public delegate void MasterChannelCallback(MasterChannelJson obj);
        private static Hashtable m_masterChannelCallbacks = new Hashtable();

        private IAsyncEnumerator<int> m_connectMasterChannelTask = null;

        public static SyncClient instance;

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

                return m_masterChannel.State == WebSocketState.Open;
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

        public class MCH_Join
        {
            public int messageType; // 0: request, 1: response, 2: broadcast
            public PrefabStore.StoreAction avatorInstantiateInfo;

            // response
            public string[] idAvails;
            public PhysicsUpdateType physicsUpdateType;
            public PrefabStore.StoreAction[] othersInstantiateInfos;
        }

        public class MCH_PhysicsUpdateType
        {
            public PhysicsUpdateType physicsUpdateType;
        }

        public class MCH_IdAvails
        {
            public int messageType; // 0: request, 1: response
            public int length;
            public string[] idAvails;
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

        public static void RegisterMasterChannelCallback(string messageType, MasterChannelCallback callback)
        {
            if (!m_masterChannelCallbacks.ContainsKey(messageType))
            {
                m_masterChannelCallbacks[messageType] = callback;
            }
        }

        private async IAsyncEnumerator<int> ConnectMasterChannelTask()
        {
            yield return 0;

            CloseMasterChannel();

            yield return 0;

            #region ADD_CALLBACKS

            RegisterMasterChannelCallback(nameof(MCH_Join), (obj) =>
            {
                var json = obj.message;

                var received = JsonUtility.FromJson<MCH_Join>(json);

                switch (received.messageType)
                {
                    case 0: // request
                        {
                            var response = new MCH_Join();
                            response.messageType = 1;
                            response.avatorInstantiateInfo = received.avatorInstantiateInfo;

                            // response
                            response.avatorInstantiateInfo.publicId = UniqueId.Generate();
                            response.idAvails = UniqueId.Generate(5);   // TODO
                            response.physicsUpdateType = PhysicsUpdateType.RECEIVER;
                            response.othersInstantiateInfos = roomAdapter.avatorInstantiateHistorys;

                            roomAdapter.UpdateState(received.avatorInstantiateInfo, out var avator);

                            MasterChannelSend(nameof(MCH_Join), JsonUtility.ToJson(response), response.avatorInstantiateInfo.userId);

                            Foreach<NetworkedObject>((networkedObject) =>
                            {
                                // SyncState
                            });
                        }
                        break;
                    case 1: // response
                        {
                            UpdatePhysicsUpdateType(received.physicsUpdateType);

                            foreach (var othersInstantiateInfo in received.othersInstantiateInfos)
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
                            roomAdapter.UpdateState(received.avatorInstantiateInfo, out var avator);
                        }
                        break;
                }
            });

            RegisterMasterChannelCallback(nameof(MCH_PhysicsUpdateType), (obj) =>
            {
                var json = obj.message;

                var received = JsonUtility.FromJson<MCH_PhysicsUpdateType>(json);

                UpdatePhysicsUpdateType(received.physicsUpdateType);
            });

            RegisterMasterChannelCallback(nameof(MCH_IdAvails), (obj) =>
            {
                var json = obj.message;

                var received = JsonUtility.FromJson<MCH_IdAvails>(json);

                switch (received.messageType)
                {
                    case 0: // request
                        break;
                    case 1: // response
                        break;
                }
            });

#if false
            m_masterChannelCallbacks[(int)WebAction.ACEPT] = (obj) => {

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
            m_masterChannelCallbacks[(int)WebAction.GUEST_DISCONNECT] = (obj) => {

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
            m_masterChannelCallbacks[(int)WebAction.GUEST_PARTICIPATION] = (obj) => {

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

            m_masterChannel = new WebSocket(roomAdapter.config.address);

            m_masterChannel.OnOpen += () =>
            {
                Debug.Log(THIS_NAME + "Connection open!");
            };

            m_masterChannel.OnError += (e) =>
            {
                Debug.Log(THIS_NAME + "Error! :" + e);
            };

            m_masterChannel.OnClose += (e) =>
            {
                Debug.Log(THIS_NAME + "Connection closed! :" + e);
            };

            m_masterChannel.OnMessage += (bytes) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);

                var obj = JsonUtility.FromJson<MasterChannelJson>(message);

                if (m_masterChannelCallbacks.ContainsKey(obj.messageType))
                {
                    var callback = m_masterChannelCallbacks[obj.messageType] as MasterChannelCallback;
                    callback.Invoke(obj);
                }
            };

            await m_masterChannel.Connect();

            m_connectMasterChannelTask = null;

            yield return 1;
        }

        public void ConnectMasterChannel()
        {
            m_connectMasterChannelTask = ConnectMasterChannelTask();
        }

        public void MasterChannelSend(string messageType, string message, int dstIndex = -1 /* -1 : broadcast */)
        {
            var obj = new MasterChannelJson
            {
                messageType = messageType,
                message = message,
                dstIndex = dstIndex,
                srcIndex = userAdapter.id,
            };
            MasterChannelSend(obj);
        }

        public void MasterChannelSend(MasterChannelJson obj)
        {
            MasterChannelSend(JsonUtility.ToJson(obj));
        }

        public async void MasterChannelSend(string json)
        {
            if (masterChannelEnabled)
            {
                await m_masterChannel.SendText(json);
            }
        }

        public async void CloseMasterChannel()
        {
            if (m_masterChannel != null)
            {
                await m_masterChannel.Close();
            }

            m_masterChannel = null;
        }

        #endregion MASTER_CHANNEL

        #region RTC_CHANNEL

        private static unsafe void LongCopy(byte* src, byte* dst, int count)
        {
            // https://github.com/neuecc/MessagePack-CSharp/issues/117

            while (count >= 8)
            {
                *(ulong*)dst = *(ulong*)src;
                dst += 8;
                src += 8;
                count -= 8;
            }

            if (count >= 4)
            {
                *(uint*)dst = *(uint*)src;
                dst += 4;
                src += 4;
                count -= 4;
            }

            if (count >= 2)
            {
                *(ushort*)dst = *(ushort*)src;
                dst += 2;
                src += 2;
                count -= 2;
            }

            if (count >= 1)
            {
                *dst = *src;
            }
        }

        public void RTCChannelOnMessage(string dst, string src, byte[] bytes)
        {
            var nameBytesLen = bytes[0];
            var subBytesStart = 1 + nameBytesLen;
            var subBytesLen = bytes.Length - subBytesStart;

            var nameBytes = new byte[nameBytesLen];

            unsafe
            {
                fixed (byte* iniP = nameBytes, iniD = bytes)    // id
                {
                    LongCopy(iniD + 1, iniP, nameBytesLen);
                }
            }

            var targetName = System.Text.Encoding.UTF8.GetString(nameBytes);

            var networkedObject = NetworkedObject.GetById(targetName);
            if (networkedObject == null)
            {
                Debug.LogError($"Networked object not found: {targetName}");

                return;
            }

            var subBytes = new byte[subBytesLen];
            System.Array.Copy(bytes, subBytesStart, subBytes, 0, subBytesLen);

            networkedObject.OnRTCMessage(dst, src, subBytes);
        }

        public void RTCChannelSend(byte[] bytes)
        {
            if (rtcChannelEnabled)
            {
                m_rtcChannel.DataChannelSend(bytes);
            }
        }

        public void CloseRTCChannel()
        {
            if (m_rtcChannel != null)
            {
                // TODO:
                //m_rtcChannel.Exit();
            }

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

        private async void Update()
        {
            if (m_connectMasterChannelTask != null)
            {
                await m_connectMasterChannelTask.MoveNextAsync();
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            m_masterChannel?.DispatchMessageQueue();
#endif
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
