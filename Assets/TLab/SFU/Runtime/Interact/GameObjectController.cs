using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TLab.SFU.Network;
using static TLab.SFU.ComponentExtension;

namespace TLab.SFU.Interact
{
    using Registry = Network.Registry<GameObjectController>;

    [AddComponentMenu("TLab/SFU/Game Object Controller (TLab)")]
    public class GameObjectController : NetworkTransform
    {
        public enum HandType
        {
            MAIN_HAND,
            SUB_HAND,
            NONE
        };

        public class GrabState
        {
            public static int FREE = -1;

            public enum Action
            {
                GRAB,
                FREE
            };

            private int m_grabberId = FREE;

            public int grabberId => m_grabberId;

            public bool grabbed => m_grabberId != FREE;

            public bool isFree => !grabbed;

            public bool grabbByMe => grabbed && NetworkClient.IsOwn(m_grabberId);

            public void Update(int grabberId) => m_grabberId = grabberId;

            public void Update(Action action)
            {
                switch (action)
                {
                    case Action.GRAB:
                        m_grabberId = NetworkClient.userId;
                        break;
                    case Action.FREE:
                        m_grabberId = FREE;
                        break;
                }
            }
        }

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [Header("Exclusive Sync Settings")]
        [SerializeField] protected bool m_locked = false;

        [Header("Transform Module")]
        [SerializeField] private PositionLogic m_position;
        [SerializeField] private RotationLogic m_rotation;
        [SerializeField] private ScaleLogic m_scale;

        [Header("Divided Settings")]
        [SerializeField] protected bool m_enableDivide = false;
        [SerializeField] protected MeshCollider m_meshCollider;
        [SerializeField] protected GameObject[] m_divideTargets;

        protected List<CashTransform> m_cashTransforms = new List<CashTransform>();

        private GrabState m_grabState = new GrabState();

        private Interactor m_mainHand;
        private Interactor m_subHand;

        public static new bool msgCallbackRegisted = false;

        public GrabState grabState => m_grabState;

        public bool locked => m_locked;

        public Interactor mainHand => m_mainHand;

        public Interactor subHand => m_subHand;

        public PositionLogic position => m_position;

        public RotationLogic rotation => m_rotation;

        public ScaleLogic scale => m_scale;

        public bool enableDivide => m_enableDivide;

        public GameObject[] divideTargets => m_divideTargets;

#if UNITY_EDITOR
        public void InitializeGameObjectRotatable()
        {
            if (EditorApplication.isPlaying)
                return;

            var rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
        }
#endif

        private void MainHandGrabbStart()
        {
            m_position.OnMainHandGrabbed(m_mainHand);
            m_rotation.OnMainHandGrabbed(m_mainHand);
            m_scale.OnMainHandGrabbed(m_mainHand);
        }

        private void SubHandGrabbStart()
        {
            m_position.OnSubHandGrabbed(m_subHand);
            m_rotation.OnSubHandGrabbed(m_subHand);
            m_scale.OnSubHandGrabbed(m_subHand);
        }

        private void MainHandGrabbEnd()
        {
            m_position.OnMainHandReleased(m_mainHand);
            m_rotation.OnMainHandReleased(m_mainHand);
            m_scale.OnMainHandReleased(m_mainHand);
        }

        private void SubHandGrabbEnd()
        {
            m_position.OnSubHandReleased(m_subHand);
            m_rotation.OnSubHandReleased(m_subHand);
            m_scale.OnSubHandReleased(m_subHand);
        }

        #region MESSAGE

        [System.Serializable]
        public class MSG_DivideGrabber : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_DivideGrabber() => pktId = MD5From(nameof(MSG_DivideGrabber));

            public Address64 networkId;
            public int grabberId;
            public bool active;

            public MSG_DivideGrabber() : base() { }

            public MSG_DivideGrabber(byte[] bytes) : base() { }
        }

        [System.Serializable]
        public class MSG_GrabbLock : Packetable
        {
            [System.Serializable]
            public enum Action
            {
                FORCE_RELEASE,
                GRAB_LOCK,
                NONE
            };

            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_GrabbLock() => pktId = MD5From(nameof(MSG_GrabbLock));

            public Address64 networkId;
            public int grabberId;
            public Action action;

            public MSG_GrabbLock() : base() { }

            public MSG_GrabbLock(byte[] bytes) : base(bytes) { }
        }

        #endregion MESSAGE

        public void GrabbLock(GrabState.Action action)
        {
            m_grabState.Update(action);

            switch (action)
            {
                case GrabState.Action.GRAB:
                    EnableRigidbody(false);
                    break;
                case GrabState.Action.FREE:
                    EnableRigidbody(true);
                    break;
            }

            SyncViaWebSocket();

            var @object = new MSG_GrabbLock
            {
                networkId = m_networkId.id,
                grabberId = m_grabState.grabberId,
                action = MSG_GrabbLock.Action.GRAB_LOCK,
            };

            NetworkClient.instance.SendWS(@object.Marshall());
        }

        public override void OnPhysicsRoleChange()
        {
            if (m_grabState.grabbed)
                return;

            switch (NetworkClient.physicsRole)
            {
                case NetworkClient.PhysicsRole.SEND:
                    EnableRigidbody(true);
                    break;
                case NetworkClient.PhysicsRole.RECV:
                    EnableRigidbody(false, true);
                    break;
            }
        }

        public override void EnableRigidbody(bool active, bool force = false)
        {
            if (force || (NetworkClient.physicsRole == NetworkClient.PhysicsRole.SEND))
                base.EnableRigidbody(active);
        }

        public void GrabbLock(int index)
        {
            if (index != GrabState.FREE)
            {
                if (m_mainHand != null)
                {
                    m_mainHand = null;
                    m_subHand = null;
                }

                m_grabState.Update(index);

                EnableRigidbody(false);
            }
            else
            {
                m_grabState.Update(GrabState.Action.FREE);

                EnableRigidbody(true);
            }
        }

        public void ForceRelease(bool self)
        {
            if (m_mainHand != null)
            {
                m_mainHand = null;
                m_subHand = null;
                m_grabState.Update(GrabState.Action.FREE);

                EnableRigidbody(false);
            }

            if (self)
            {
                var @object = new MSG_GrabbLock
                {
                    networkId = m_networkId.id,
                    grabberId = m_grabState.grabberId,
                    action = MSG_GrabbLock.Action.FORCE_RELEASE,
                };

                NetworkClient.instance.SendWS(@object.Marshall());
            }
        }

        private void CreateCombinedMeshCollider()
        {
            var meshColliders = GetComponentsInTargets<MeshCollider>(divideTargets);

            var combine = new CombineInstance[meshColliders.Length];

            for (int i = 0; i < meshColliders.Length; i++)
            {
                combine[i].mesh = meshColliders[i].sharedMesh;
                combine[i].transform = gameObject.transform.worldToLocalMatrix * meshColliders[i].transform.localToWorldMatrix;
            }

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine);

            if (m_meshCollider != null)
                m_meshCollider.sharedMesh = mesh;
        }

        public void Divide(bool active)
        {
            if (!m_enableDivide)
                return;

            var meshCollider = this.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError(THIS_NAME + "Mesh Collider Not Found");
                return;
            }

            meshCollider.enabled = !active;

            GetComponentsInTargets<MeshCollider>(divideTargets).Foreach((c) => c.enabled = active);

            gameObject.Foreach<GameObjectRotatable>((c) => c.Stop());

            GetComponentsInTargets<GameObjectController>(divideTargets).Foreach((c) => c.ForceRelease(true));

            if (!active)
                CreateCombinedMeshCollider();
        }

        public void Devide()
        {
            if (!m_enableDivide)
                return;

            var meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError(THIS_NAME + "Mesh Collider Not Found");
                return;
            }

            var current = meshCollider.enabled;
            var divide = current;

            Divide(divide);
        }

        public void SetInitialChildTransform()
        {
            if (!m_enableDivide)
                return;

            int index = 0;

            GetComponentsInTargets<Transform>(divideTargets).Foreach((c) =>
            {
                var cash = m_cashTransforms[index++];

                c.localPosition = cash.LocalPosiiton;
                c.localRotation = cash.LocalRotation;
                c.localScale = cash.LocalScale;
            });

            gameObject.Foreach<GameObjectRotatable>((c) => c.Stop());

            var meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError(THIS_NAME + "Mesh Collider Not Found");
                return;
            }

            if (meshCollider.enabled)
                CreateCombinedMeshCollider();
        }

        private void GetInitialChildTransform()
        {
            if (m_enableDivide)
            {
                m_cashTransforms.Clear();

                GetComponentsInTargets<Transform>(divideTargets).Foreach((c) => m_cashTransforms.Add(new CashTransform(c.localPosition, c.localScale, c.localRotation)));

                CreateCombinedMeshCollider();
            }
        }

        public HandType GetHandType(Interactor interactor)
        {
            if (m_mainHand == interactor)
                return HandType.MAIN_HAND;

            if (m_subHand == interactor)
                return HandType.SUB_HAND;

            return HandType.NONE;
        }

        public HandType OnGrab(Interactor interactor)
        {
            if (m_locked || (!m_grabState.isFree && !m_grabState.grabbByMe))
                return HandType.NONE;

            if (m_mainHand == null)
            {
                GrabbLock(GrabState.Action.GRAB);

                m_mainHand = interactor;

                MainHandGrabbStart();

                return HandType.MAIN_HAND;
            }
            else if (m_subHand == null)
            {
                m_subHand = interactor;

                SubHandGrabbStart();

                return HandType.SUB_HAND;
            }

            return HandType.NONE;
        }

        public bool OnRelease(Interactor interactor)
        {
            if (m_mainHand == interactor)
            {
                MainHandGrabbEnd();

                if (m_subHand != null)
                {
                    m_mainHand = m_subHand;
                    m_subHand = null;

                    MainHandGrabbStart();

                    return true;
                }
                else
                {
                    GrabbLock(GrabState.Action.FREE);

                    m_mainHand = null;

                    return false;
                }
            }
            else if (m_subHand == interactor)
            {
                SubHandGrabbEnd();

                m_subHand = null;

                MainHandGrabbStart();

                return false;
            }

            return false;
        }

        public override void Init(Address32 publicId)
        {
            base.Init(publicId);

            Registry.Register(m_networkId.id, this);
        }

        public override void Init()
        {
            base.Init();

            Registry.Register(m_networkId.id, this);
        }

        public override void Shutdown()
        {
            if (m_grabState.grabbByMe)
                GrabbLock(GrabState.Action.FREE);

            if (m_networkId)
                Registry.UnRegister(m_networkId.id);

            base.Shutdown();
        }

        protected override void Awake()
        {
            base.Awake();

            if (!msgCallbackRegisted)
            {
                NetworkClient.RegisterOnMessage(MSG_GrabbLock.pktId, (from, to, bytes) =>
                {
                    var @object = new MSG_GrabbLock(bytes);

                    switch (@object.action)
                    {
                        case MSG_GrabbLock.Action.GRAB_LOCK:
                            Registry.GetById(@object.networkId)?.GrabbLock(@object.grabberId);
                            break;
                        case MSG_GrabbLock.Action.FORCE_RELEASE:
                            Registry.GetById(@object.networkId)?.ForceRelease(false);
                            break;
                        default:
                            break;
                    }
                });

                NetworkClient.RegisterOnMessage(MSG_DivideGrabber.pktId, (from, to, bytes) =>
                {
                    var @object = new MSG_DivideGrabber(bytes);
                    Registry.GetById(@object.networkId)?.Divide(@object.active);
                });

                msgCallbackRegisted = true;
            }
        }

        protected override void Start()
        {
            base.Start();

            GetInitialChildTransform();

            m_position.Start(this.transform, m_rb);
            m_rotation.Start(this.transform, m_rb);
            m_scale.Start(this.transform, m_rb);
        }

        protected override void Update()
        {
            if (m_mainHand != null)
            {
                if (m_subHand != null)
                {
                    m_position.UpdateTwoHandLogic();
                    m_scale.UpdateTwoHandLogic();
                }
                else
                {
                    m_position.UpdateOneHandLogic();
                    m_rotation.UpdateOneHandLogic();
                }
            }
            else if (m_grabState.isFree)
                m_scale.UpdateHandleLogic();

            base.Update();
        }

        protected override void Register()
        {
            base.Register();

            Registry.Register(m_networkId.id, this);
        }

        protected override void UnRegister()
        {
            Registry.UnRegister(m_networkId.id);

            base.UnRegister();
        }
    }
}
