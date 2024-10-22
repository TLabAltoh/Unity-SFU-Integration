using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using TLab.SFU.Network;
using static TLab.SFU.ComponentExtention;

namespace TLab.SFU.Interact
{
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

        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static Hashtable registry => m_registry;

        protected static void Register(Address64 id, GameObjectController controller)
        {
            if (!m_registry.ContainsKey(id))
                m_registry.Add(id, controller);
        }

        protected static new void UnRegister(Address64 id)
        {
            if (m_registry.ContainsKey(id))
                m_registry.Remove(id);
        }

        public static new void ClearRegistry()
        {
            var gameObjects = m_registry.Values.Cast<GameObjectController>().Select((t) => t.gameObject);

            foreach (var gameObject in gameObjects)
                Destroy(gameObject);

            m_registry.Clear();
        }

        public static new GameObjectController GetById(Address64 id) => m_registry[id] as GameObjectController;

        #endregion REGISTRY

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

            Register(m_networkedId.id, this);
        }

        public override void Init()
        {
            base.Init();

            Register(m_networkedId.id, this);
        }

        #region MESSAGE_TYPE

        [System.Serializable]
        public struct MCH_DivideGrabber : Packetable
        {
            public static int pktId;

            static MCH_DivideGrabber() => pktId = nameof(MCH_DivideGrabber).GetHashCode();

            public Address64 networkedId;
            public int grabberId;
            public bool active;

            public byte[] Marshall()
            {
                var json = JsonUtility.ToJson(this);
                return UnsafeUtility.Combine(pktId, Encoding.UTF8.GetBytes(json));
            }

            public void UnMarshall(byte[] bytes)
            {
                var json = Encoding.UTF8.GetString(bytes, SyncClient.HEADER_SIZE, bytes.Length - SyncClient.HEADER_SIZE);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }

        [System.Serializable]
        public struct MCH_GrabbLock : Packetable
        {
            [System.Serializable]
            public enum Action
            {
                FORCE_RELEASE,
                GRAB_LOCK,
                NONE
            };

            public static int pktId;

            static MCH_GrabbLock() => pktId = nameof(MCH_GrabbLock).GetHashCode();

            public Address64 networkedId;
            public int grabberId;
            public Action action;

            public byte[] Marshall()
            {
                var json = JsonUtility.ToJson(this);
                return UnsafeUtility.Combine(pktId, Encoding.UTF8.GetBytes(json));
            }

            public void UnMarshall(byte[] bytes)
            {
                var json = Encoding.UTF8.GetString(bytes, SyncClient.HEADER_SIZE, bytes.Length - SyncClient.HEADER_SIZE);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }

        #endregion MESSAGE_TYPE

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

            var @object = new MCH_GrabbLock
            {
                networkedId = m_networkedId.id,
                grabberId = m_grabState.grabberId,
                action = MCH_GrabbLock.Action.GRAB_LOCK,
            };

            SyncClient.instance.MasterChannelSend(@object.Marshall());
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
                var @object = new MCH_GrabbLock
                {
                    networkedId = m_networkedId.id,
                    grabberId = m_grabState.grabberId,
                    action = MCH_GrabbLock.Action.FORCE_RELEASE,
                };

                SyncClient.instance.MasterChannelSend(@object.Marshall());
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
                SyncClient.RegisterMasterChannelCallback(MCH_GrabbLock.pktId, (from, bytes) =>
                {
                    var @object = new MCH_GrabbLock();
                    @object.UnMarshall(bytes);

                    switch (@object.action)
                    {
                        case MCH_GrabbLock.Action.GRAB_LOCK:
                            GetById(@object.networkedId)?.GrabbLock(@object.grabberId);
                            break;
                        case MCH_GrabbLock.Action.FORCE_RELEASE:
                            GetById(@object.networkedId)?.ForceRelease(false);
                            break;
                        default:
                            break;
                    }
                });

                SyncClient.RegisterMasterChannelCallback(MCH_DivideGrabber.pktId, (from, bytes) =>
                {
                    var @object = new MCH_DivideGrabber();
                    @object.UnMarshall(bytes);

                    GetById(@object.networkedId)?.Divide(@object.active);
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

            Register(m_networkedId.id, this);
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

            UnRegister(m_networkedId.id);
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
