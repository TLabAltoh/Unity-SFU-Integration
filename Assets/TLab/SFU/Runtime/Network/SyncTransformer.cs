using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Text;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Sync Transformer (TLab)")]
    public class SyncTransformer : NetworkedObject
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        protected static void Register(Address64 id, SyncTransformer syncTransformer)
        {
            if (!m_registry.ContainsKey(id))
                m_registry.Add(id, syncTransformer);
        }

        protected static new void UnRegister(Address64 id)
        {
            if (m_registry.ContainsKey(id))
                m_registry.Remove(id);
        }

        public static new void ClearRegistry()
        {
            var gameObjects = m_registry.Values.Cast<SyncTransformer>().Select((t) => t.gameObject);

            foreach (var gameObject in gameObjects)
                Destroy(gameObject);

            m_registry.Clear();
        }

        public static new void Destroy(GameObject go)
        {
            if (go.GetComponent<SyncTransformer>() != null)
                Destroy(go);
        }

        public static new void Destroy(Address64 id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
                Destroy(go);
        }

        public static new SyncTransformer GetById(Address64 id) => m_registry[id] as SyncTransformer;

        #endregion REGISTRY

        #region STRUCT

        [System.Serializable]
        public struct RigidbodyState
        {
            [SerializeField] private bool m_used;

            [SerializeField] private bool m_gravity;

            public bool used => m_used;

            public bool gravity => m_gravity;

            public RigidbodyState(bool used, bool gravity)
            {
                m_used = used;
                m_gravity = gravity;
            }
        }

        [System.Serializable]
        public struct WebTransformerState
        {
            public Address64 id;
            public WebVector3 position;
            public WebVector4 rotation;
            public WebVector3 scale;
            public RigidbodyState rb;
        }

        #endregion STRUCT

        #region MESSAGE_TYPE

        [System.Serializable]
        public struct MCH_SyncTransform : Packetable
        {
            public static int pktId;

            static MCH_SyncTransform() => pktId = nameof(MCH_SyncTransform).GetHashCode();

            public Address64 networkedId;
            public WebTransformerState transformState;

            public byte[] Marshall()
            {
                var json = JsonUtility.ToJson(this);
                return UnsafeUtility.Combine(pktId, Encoding.UTF8.GetBytes(json));
            }

            public void UnMarshall(byte[] bytes)
            {
                var json = Encoding.UTF8.GetString(bytes, SyncClient.PAYLOAD_OFFSET, bytes.Length - SyncClient.PAYLOAD_OFFSET);
                JsonUtility.FromJsonOverwrite(json, this);
            }
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

        public override void OnReceive(int to, int from, byte[] payload)
        {
            float[] rtcTransform = new float[10];

            unsafe
            {
                fixed (byte* payloadPtr = payload)
                fixed (float* rtcTransformPtr = &(rtcTransform[0]))
                {
                    UnsafeUtility.LongCopy(payloadPtr + 2, (byte*)rtcTransformPtr, rtcTransform.Length * sizeof(float));

                    var transformerState = new WebTransformerState
                    {
                        id = m_networkedId.id,
                        rb = new RigidbodyState(*((bool*)&(payloadPtr[0])), *((bool*)&(payloadPtr[1]))),
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

            var rtcTransform = new float[10];

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

            int headerLen = 8;  // Address64
            int payloadLen = 2 + rtcTransform.Length * sizeof(float);  // rbState.used (1) + rbState.gravity (1) + transform ((3 + 4 + 3) * 4)

            var packet = new byte[headerLen + payloadLen];

            unsafe
            {
                fixed (byte* packetPtr = packet)
                {
                    m_networkedId.id.CopyTo(packetPtr);

                    bool rbUsed = m_rbState.used, rbGravity = m_rbState.gravity;

                    packetPtr[headerLen + 0] = (byte)(&rbUsed);
                    packetPtr[headerLen + 1] = (byte)(&rbGravity);

                    fixed (float* transformPtr = &(rtcTransform[0]))
                        UnsafeUtility.LongCopy((byte*)transformPtr, packetPtr + headerLen + 2, payloadLen - 2);
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

                rb = m_rbState,

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

            var @object = new MCH_SyncTransform
            {
                networkedId = m_networkedId.id,
                transformState = transformerState,
            };

            SyncClient.instance.MasterChannelSend(@object.Marshall());

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

        public override void Init(Address32 publicId)
        {
            base.Init(publicId);

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
                SyncClient.RegisterMasterChannelCallback(MCH_SyncTransform.pktId, (from, bytes) =>
                {
                    var @object = new MCH_SyncTransform();
                    @object.UnMarshall(bytes);
                    GetById(@object.networkedId)?.SyncTransformFromOutside(@object.transformState);
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
