using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Sync Transformer (TLab)")]
    public class SyncTransformer : NetworkedObject
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        protected static void Register(string id, SyncTransformer syncTransformer)
        {
            if (!m_registry.ContainsKey(id))
            {
                m_registry[id] = syncTransformer;
            }
        }

        protected static new void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
            {
                m_registry.Remove(id);
            }
        }

        public static new void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var syncTransformer = entry.Value as SyncTransformer;
                gameobjects.Add(syncTransformer.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static new void Destroy(GameObject go)
        {
            if (go.GetComponent<SyncTransformer>() != null)
            {
                Destroy(go);
            }
        }

        public static new void Destroy(string id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
            {
                Destroy(go);
            }
        }

        public static new SyncTransformer GetById(string id) => m_registry[id] as SyncTransformer;

        #endregion REGISTRY

        #region STRUCT

        public struct RigidbodyState
        {
            private bool m_used;

            private bool m_gravity;

            public bool used => m_used;

            public bool gravity => m_gravity;

            public RigidbodyState(bool used, bool gravity)
            {
                m_used = used;
                m_gravity = gravity;
            }
        }

        [System.Serializable]
        public class WebTransformerState
        {
            public string id;
            public bool rigidbody;
            public bool gravity;
            public WebVector3 position;
            public WebVector4 rotation;
            public WebVector3 scale;
        }

        #endregion STRUCT

        #region MESSAGE_TYPE

        [System.Serializable]
        public class MCH_SyncTransform
        {
            public string networkedId;
            public WebTransformerState transformState;
        }

        #endregion MESSAGE_TYPE

        protected Rigidbody m_rb;

        protected RigidbodyState m_rbState;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        // Windows 12's Core i 9: 400 -----> Size: 20
        protected const int CASH_COUNT = 20;
#else
        // Oculsu Quest 2: 72 -----> Size: 20 * 72 / 400 = 3.6 ~= 4
        protected const int CASH_COUNT = 5;
#endif

        protected FixedQueue<Vector3> m_prevPoss = new FixedQueue<Vector3>(CASH_COUNT);

        protected FixedQueue<Quaternion> m_prevRots = new FixedQueue<Quaternion>(CASH_COUNT);

        public Rigidbody rb => m_rb;

        public RigidbodyState rbState => m_rbState;

        public bool enableGravity => (m_rb == null) ? false : m_rb.useGravity;

        public static bool mchCallbackRegisted = false;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

#if UNITY_EDITOR
        public virtual void UseRigidbody(bool rigidbody, bool gravity)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = gravity;
        }
#endif

        protected Vector3 GuessVelocity()
        {
            if (m_prevPoss.Count < 2)
            {
                return Vector3.zero;
            }

            var prevPoss = m_prevPoss.ToArray();

            m_prevPoss.Clear();

            var diff = Vector3.zero;

            for (int i = 1; i < prevPoss.Length; i++)
            {
                diff += prevPoss[i] - prevPoss[i - 1];
            }

            return diff / (prevPoss.Length - 1) / Time.deltaTime;
        }

        protected Vector3 GuessAngulerVelocity()
        {
            if (m_prevRots.Count < 2)
            {
                return Vector3.zero;
            }

            var prevRots = m_prevRots.ToArray();

            m_prevRots.Clear();

            var diff0 = Vector3.zero;

            for (int i = 1; i < prevRots.Length; i++)
            {
                var diff1 = Quaternion.Inverse(prevRots[i - 1]) * prevRots[i];

                diff1.ToAngleAxis(out var angle, out var axis);

                diff0 += (m_rb.rotation * axis) * angle;
            }

            return diff0 / (prevRots.Length - 1) / Time.deltaTime;
        }

        public virtual void SetGravity(bool active)
        {
            if (m_rb == null)
            {
                return;
            }

            if (active)
            {
                m_rb.isKinematic = false;
                m_rb.useGravity = true;

                m_rb.velocity = GuessVelocity();
                m_rb.angularVelocity = GuessAngulerVelocity();
            }
            else
            {
                m_rb.isKinematic = true;
                m_rb.useGravity = false;
                m_rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        public void SyncTransformFromOutside(WebTransformerState transformerState)
        {
            var position = transformerState.position;
            var scale = transformerState.scale;
            var rotation = transformerState.rotation;

            transform.localScale = new Vector3(scale.x, scale.y, scale.z);

            if (m_rb != null)
            {
                m_rb.MovePosition(new Vector3(position.x, position.y, position.z));
                m_rb.MoveRotation(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
            }
            else
            {
                transform.position = new Vector3(position.x, position.y, position.z);
                transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            }

            CashRbTransform();

            m_syncFromOutside = true;
        }

        public override void OnReceive(string dst, string src, byte[] bytes)
        {
            float[] rtcTransform = new float[10];

            unsafe
            {
                fixed (byte* subBytesPtr = bytes)  // transform
                fixed (float* transformPtr = &(rtcTransform[0]))
                {
                    LongCopy(subBytesPtr + 2, (byte*)transformPtr, rtcTransform.Length * sizeof(float));

                    var transformerState = new WebTransformerState
                    {
                        id = m_networkedId.id,
                        gravity = *((bool*)&(subBytesPtr[0])),
                        rigidbody = *((bool*)&(subBytesPtr[1])),
                        position = new WebVector3 { x = rtcTransform[0], y = rtcTransform[1], z = rtcTransform[2] },
                        rotation = new WebVector4 { x = rtcTransform[3], y = rtcTransform[4], z = rtcTransform[5], w = rtcTransform[6] },
                        scale = new WebVector3 { x = rtcTransform[7], y = rtcTransform[8], z = rtcTransform[9] },
                    };

                    SyncTransformFromOutside(transformerState);
                }
            }
        }

        public virtual void SyncTransformViaWebRTC()
        {
            if (!m_enableSync)
            {
                return;
            }

            CashRbTransform();

            float[] rtcTransform = new float[10];

            rtcTransform[0] = transform.position.x;
            rtcTransform[1] = transform.position.y;
            rtcTransform[2] = transform.position.z;

            rtcTransform[3] = transform.rotation.x;
            rtcTransform[4] = transform.rotation.y;
            rtcTransform[5] = transform.rotation.z;
            rtcTransform[6] = transform.rotation.w;

            rtcTransform[7] = transform.localScale.x;
            rtcTransform[8] = transform.localScale.y;
            rtcTransform[9] = transform.localScale.z;

            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(m_networkedId.id);

            int headerBytesLen = 1 + idBytes.Length; // idLength (1) + id (...)
            int subBytesLen = 2 + rtcTransform.Length * sizeof(float);  // rbUsed (1) + rbGravity (1) + transform ((3 + 4 + 3) * 4)

            byte[] packet = new byte[headerBytesLen + subBytesLen];

            unsafe
            {
                fixed (byte* packetPtr = packet, idBytesPtr = idBytes)
                {
                    packetPtr[0] = (byte)idBytes.Length;
                    LongCopy(idBytesPtr, packetPtr + 1, idBytes.Length);

                    bool rbUsed = m_rbState.used, rbGravity = m_rbState.gravity;

                    packetPtr[headerBytesLen + 0] = (byte)(&rbUsed);
                    packetPtr[headerBytesLen + 1] = (byte)(&rbGravity);

                    fixed (float* transformPtr = &(rtcTransform[0]))
                    {
                        LongCopy((byte*)transformPtr, packetPtr + headerBytesLen + 2, subBytesLen - 2);
                    }
                }
            }

            SyncClient.instance.SendRTC(packet);

            m_syncFromOutside = false;
        }

        public virtual void SyncTransformViaWebSocket()
        {
            if (!m_enableSync)
            {
                return;
            }

            CashRbTransform();

            var transformerState = new WebTransformerState
            {
                id = m_networkedId.id,

                rigidbody = m_rbState.used,
                gravity = m_rbState.gravity,

                position = new WebVector3
                {
                    x = transform.position.x,
                    y = transform.position.y,
                    z = transform.position.z
                },
                rotation = new WebVector4
                {
                    x = transform.rotation.x,
                    y = transform.rotation.y,
                    z = transform.rotation.z,
                    w = transform.rotation.w,
                },
                scale = new WebVector3
                {
                    x = transform.localScale.x,
                    y = transform.localScale.y,
                    z = transform.localScale.z
                }
            };

            var message = JsonUtility.ToJson(transformerState);

            var obj = new MasterChannelJson
            {
                messageType = nameof(WebTransformerState),
                message = message,
            };

            SyncClient.instance.MasterChannelSend(obj);

            m_syncFromOutside = false;
        }

        protected virtual void CashRbTransform()
        {
            if (m_rb != null)
            {
                m_prevPoss.Enqueue(m_rb.position);
                m_prevRots.Enqueue(m_rb.rotation);
            }
        }

        public override void Shutdown()
        {
            if (m_state == State.SHUTDOWNED)
            {
                return;
            }

            UnRegister(m_networkedId.id);

            base.Shutdown();
        }

        protected virtual void InitRigidbody()
        {
            m_rb = GetComponent<Rigidbody>();

            if (m_rb != null)
            {
                m_rb = this.gameObject.RequireComponent<Rigidbody>();
                m_prevPoss.Enqueue(m_rb.position);
                m_prevRots.Enqueue(m_rb.rotation);

                m_rbState = new RigidbodyState(true, m_rb.useGravity);

                SetGravity(false);
            }
            else
            {
                m_rbState = new RigidbodyState(false, false);
            }
        }

        public override void Init(string id)
        {
            base.Init(id);

            InitRigidbody();

            Register(m_networkedId.id, this);
        }

        public override void Init()
        {
            base.Init();

            InitRigidbody();

            Register(m_networkedId.id, this);
        }

        protected override void Awake()
        {
            base.Awake();

            if (!mchCallbackRegisted)
            {
                SyncClient.RegisterMasterChannelCallback(nameof(MCH_SyncTransform), (obj) =>
                {
                    var json = JsonUtility.FromJson<MCH_SyncTransform>(obj.message);

                    GetById(json.networkedId)?.SyncTransformFromOutside(json.transformState);
                });

                mchCallbackRegisted = true;
            }
        }

        protected override void OnDestroy()
        {
            Shutdown();
        }

        protected override void OnApplicationQuit()
        {
            Shutdown();
        }
    }
}
