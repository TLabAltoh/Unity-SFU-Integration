using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TLab.SFU.Network;
using static TLab.SFU.ComponentExtention;

namespace TLab.SFU.Interact
{
    using Registry = Network.Registry<GameObjectController>;

    [AddComponentMenu("TLab/SFU/Game Object Controller (TLab)")]
    public class GameObjectController : SyncTransformer
    {
        public enum HandType
        {
            MAIN_HAND,
            SUB_HAND,
            NONE
        };

        public class GrabState
        {
            public enum GrabberId
            {
                FREE = -1,
            }

            public enum Action
            {
                GRABB,
                FREE
            };

            private int m_grabberId = (int)GrabberId.FREE;

            public int grabberId => m_grabberId;

            public bool grabbed => m_grabberId != (int)GrabberId.FREE;

            public bool isFree => !grabbed;

            public bool grabbByMe => grabbed && SyncClient.IsOwn(m_grabberId);

            public void Update(int grabberId)
            {
                m_grabberId = grabberId;
            }

            public void Update(Action action)
            {
                switch (action)
                {
                    case Action.GRABB:
                        m_grabberId = SyncClient.userId;
                        break;
                    case Action.FREE:
                        m_grabberId = (int)GrabberId.FREE;
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

        public static new bool mchCallbackRegisted = false;

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
            {
                return;
            }

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

        protected override void InitRigidbody()
        {
            base.InitRigidbody();

            // TODO:
        }

        public override void Init(Address32 publicId)
        {
            base.Init(publicId);

            Registry.Register(m_networkedId.id, this);
        }

        public override void Init()
        {
            base.Init();

            Registry.Register(m_networkedId.id, this);
        }

        #region MESSAGE

        [System.Serializable]
        public class MSG_DivideGrabber : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_DivideGrabber() => pktId = MD5From(nameof(MSG_DivideGrabber));

            public Address64 networkedId;
            public int grabberId;
            public bool active;
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

            public Address64 networkedId;
            public int grabberId;
            public Action action;
        }

        #endregion MESSAGE

        public void GrabbLock(GrabState.Action action)
        {
            m_grabState.Update(action);

            if (SyncClient.physicsUpdateType == SyncClient.PhysicsUpdateType.SENDER)
            {
                switch (action)
                {
                    case GrabState.Action.GRABB:
                        SetGravity(false);
                        break;
                    case GrabState.Action.FREE:
                        SetGravity(true);
                        break;
                }
            }

            SyncTransformViaWebSocket();

            var @object = new MSG_GrabbLock
            {
                networkedId = m_networkedId.id,
                grabberId = m_grabState.grabberId,
                action = MSG_GrabbLock.Action.GRAB_LOCK,
            };

            SyncClient.instance.SendWS(@object.Marshall());
        }

        public void GrabbLock(int index)
        {
            if (index != (int)GrabState.GrabberId.FREE)
            {
                if (m_mainHand != null)
                {
                    m_mainHand = null;
                    m_subHand = null;
                }

                m_grabState.Update(index);

                if (SyncClient.physicsUpdateType == SyncClient.PhysicsUpdateType.SENDER)
                {
                    SetGravity(false);
                }
            }
            else
            {
                m_grabState.Update(GrabState.Action.FREE);

                if (SyncClient.physicsUpdateType == SyncClient.PhysicsUpdateType.SENDER)
                {
                    SetGravity(true);
                }
            }
        }

        public void ForceRelease(bool self)
        {
            if (m_mainHand != null)
            {
                m_mainHand = null;
                m_subHand = null;
                m_grabState.Update(GrabState.Action.FREE);

                SetGravity(false);
            }

            if (self)
            {
                var @object = new MSG_GrabbLock
                {
                    networkedId = m_networkedId.id,
                    grabberId = m_grabState.grabberId,
                    action = MSG_GrabbLock.Action.FORCE_RELEASE,
                };

                SyncClient.instance.SendWS(@object.Marshall());
            }
        }

        private void CreateCombineMeshCollider()
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
            {
                m_meshCollider.sharedMesh = mesh;
            }
        }

        public void Divide(bool active)
        {
            if (!m_enableDivide)
            {
                return;
            }

            var meshCollider = this.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError(THIS_NAME + "Mesh Collider Not Found");
                return;
            }

            meshCollider.enabled = !active;

            var childs = GetComponentsInTargets<MeshCollider>(divideTargets);
            foreach (var child in childs)
            {
                child.enabled = active;
            }

            var rotatables = this.gameObject.GetComponentsInChildren<GameObjectRotatable>();
            foreach (var rotatable in rotatables)
            {
                rotatable.Stop();
            }

            var controllers = GetComponentsInTargets<GameObjectController>(divideTargets);
            foreach (var controller in controllers)
            {
                controller.ForceRelease(true);
            }

            if (!active)
            {
                CreateCombineMeshCollider();
            }
        }

        public void Devide()
        {
            if (!m_enableDivide)
            {
                return;
            }

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
            {
                return;
            }

            int index = 0;

            var childTransforms = GetComponentsInTargets<Transform>(divideTargets);
            foreach (var childTransform in childTransforms)
            {
                var cashTransform = m_cashTransforms[index++];

                childTransform.localPosition = cashTransform.LocalPosiiton;
                childTransform.localRotation = cashTransform.LocalRotation;
                childTransform.localScale = cashTransform.LocalScale;
            }

            var rotatables = this.gameObject.GetComponentsInChildren<GameObjectRotatable>();
            foreach (var rotatable in rotatables)
            {
                rotatable.Stop();
            }

            var meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError(THIS_NAME + "Mesh Collider Not Found");
                return;
            }

            if (meshCollider.enabled)
            {
                CreateCombineMeshCollider();
            }
        }

        private void GetInitialChildTransform()
        {
            if (m_enableDivide)
            {
                m_cashTransforms.Clear();

                var childTransforms = GetComponentsInTargets<Transform>(divideTargets);
                foreach (var childTransform in childTransforms)
                {
                    m_cashTransforms.Add(new CashTransform(
                        childTransform.localPosition,
                        childTransform.localScale,
                        childTransform.localRotation));
                }

                CreateCombineMeshCollider();
            }
        }

        public HandType GetHandType(Interactor interactor)
        {
            if (m_mainHand == interactor)
            {
                return HandType.MAIN_HAND;
            }

            if (m_subHand == interactor)
            {
                return HandType.SUB_HAND;
            }

            return HandType.NONE;
        }

        public HandType OnGrabbed(Interactor interactor)
        {
            if (m_locked || (!m_grabState.isFree && !m_grabState.grabbByMe))
            {
                return HandType.NONE;
            }

            if (m_mainHand == null)
            {
                GrabbLock(GrabState.Action.GRABB);

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

        protected override void Awake()
        {
            base.Awake();

            if (!mchCallbackRegisted)
            {
                SyncClient.RegisterOnMessage(MSG_GrabbLock.pktId, (from, to, bytes) =>
                {
                    var @object = new MSG_GrabbLock();
                    @object.UnMarshall(bytes);

                    switch (@object.action)
                    {
                        case MSG_GrabbLock.Action.GRAB_LOCK:
                            Registry.GetById(@object.networkedId)?.GrabbLock(@object.grabberId);
                            break;
                        case MSG_GrabbLock.Action.FORCE_RELEASE:
                            Registry.GetById(@object.networkedId)?.ForceRelease(false);
                            break;
                        default:
                            break;
                    }
                });

                SyncClient.RegisterOnMessage(MSG_DivideGrabber.pktId, (from, to, bytes) =>
                {
                    var @object = new MSG_DivideGrabber();
                    @object.UnMarshall(bytes);
                    Registry.GetById(@object.networkedId)?.Divide(@object.active);
                });

                mchCallbackRegisted = true;
            }
        }

        protected override void Start()
        {
            base.Start();

            GetInitialChildTransform();

            m_position.Start(this.transform, m_rb);
            m_rotation.Start(this.transform, m_rb);
            m_scale.Start(this.transform, m_rb);

            Registry.Register(m_networkedId.id, this);
        }

        protected override void Update()
        {
            base.Update();

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

                SyncTransformViaWebRTC();
            }
            else
            {
                if (m_grabState.isFree && m_scale.UpdateHandleLogic())
                {
                    SyncTransformViaWebRTC();
                }
            }
        }

        public override void Shutdown()
        {
            if (m_grabState.grabbByMe)
            {
                GrabbLock(GrabState.Action.FREE);
            }

            Registry.UnRegister(m_networkedId.id);
        }

        protected override void OnDestroy()
        {
            Shutdown();

            base.OnDestroy();
        }

        protected override void OnApplicationQuit()
        {
            Shutdown();

            base.OnApplicationQuit();
        }
    }
}
