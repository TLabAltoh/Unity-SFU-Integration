using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Sync Transformer (TLab)")]
    public class SyncTransformer : NetworkedObject
    {
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

            public void Update(bool used, bool gravity)
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

        #region MESSAGE

        [System.Serializable]
        public struct MSG_SyncTransform : Packetable
        {
            public static int pktId;

            static MSG_SyncTransform() => pktId = nameof(MSG_SyncTransform).GetHashCode();

            public WebTransformerState transformerState;

            public const int HEADER_LEN = 8;

            public const int PAYLOAD_LEN = 2 + 10 * sizeof(float);  // rbState.used (1) + rbState.gravity (1) + transform ((3 + 4 + 3) * 4)

            public byte[] Marshall()
            {
                var rtcTransform = new float[10];

                rtcTransform[0] = transformerState.position.x;
                rtcTransform[1] = transformerState.position.y;
                rtcTransform[2] = transformerState.position.z;

                rtcTransform[3] = transformerState.rotation.x;
                rtcTransform[4] = transformerState.rotation.y;
                rtcTransform[5] = transformerState.rotation.z;
                rtcTransform[6] = transformerState.rotation.w;

                rtcTransform[7] = transformerState.scale.x;
                rtcTransform[8] = transformerState.scale.y;
                rtcTransform[9] = transformerState.scale.z;

                var packet = new byte[HEADER_LEN + PAYLOAD_LEN];

                unsafe
                {
                    fixed (byte* packetPtr = packet)
                    {
                        transformerState.id.CopyTo(packetPtr);

                        bool rbUsed = transformerState.rb.used, rbGravity = transformerState.rb.gravity;

                        packetPtr[HEADER_LEN + 0] = (byte)(&rbUsed);
                        packetPtr[HEADER_LEN + 1] = (byte)(&rbGravity);

                        fixed (float* transformPtr = &(rtcTransform[0]))
                            UnsafeUtility.LongCopy((byte*)transformPtr, packetPtr + HEADER_LEN + 2, PAYLOAD_LEN - 2);
                    }
                }

                return packet;
            }

            public void UnMarshall(byte[] bytes)
            {
                float[] rtcTransform = new float[10];

                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    fixed (float* rtcTransformPtr = &(rtcTransform[0]))
                    {
                        UnsafeUtility.LongCopy(bytesPtr + HEADER_LEN + 2, (byte*)rtcTransformPtr, PAYLOAD_LEN - 2);

                        transformerState.id.Copy(bytesPtr);
                        transformerState.rb.Update(*((bool*)&(bytesPtr[HEADER_LEN + 0])), *((bool*)&(bytesPtr[HEADER_LEN + 1])));

                        transformerState.position.x = rtcTransform[0];
                        transformerState.position.y = rtcTransform[1];
                        transformerState.position.z = rtcTransform[2];

                        transformerState.rotation.x = rtcTransform[3];
                        transformerState.rotation.y = rtcTransform[4];
                        transformerState.rotation.z = rtcTransform[5];
                        transformerState.rotation.w = rtcTransform[6];

                        transformerState.scale.x = rtcTransform[7];
                        transformerState.scale.y = rtcTransform[8];
                        transformerState.scale.z = rtcTransform[9];
                    }
                }
            }
        }

        #endregion MESSAGE

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

        public virtual void SyncTransformViaWebRTC()
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

            var @object = new MSG_SyncTransform
            {
                transformerState = transformerState,
            };

            SyncClient.instance.SendWS(@object.Marshall());

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

            var @object = new MSG_SyncTransform
            {
                transformerState = transformerState,
            };

            SyncClient.instance.SendWS(@object.Marshall());

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

            Registry<SyncTransformer>.UnRegister(m_networkedId.id);

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

            Registry<SyncTransformer>.Register(m_networkedId.id, this);
        }

        public override void Init()
        {
            base.Init();

            InitRigidbody();

            Registry<SyncTransformer>.Register(m_networkedId.id, this);
        }

        protected override void Awake()
        {
            base.Awake();

            if (!mchCallbackRegisted)
            {
                SyncClient.RegisterOnMessage(MSG_SyncTransform.pktId, (from, to, bytes) =>
                {
                    var @object = new MSG_SyncTransform();
                    @object.UnMarshall(bytes);
                    Registry<SyncTransformer>.GetById(@object.transformerState.id)?.SyncTransformFromOutside(@object.transformerState);
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
