using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using TLab.SFU.Network;
using static TLab.SFU.ComponentExtension;

namespace TLab.SFU.Interact
{
    using Registry = Registry<Address64, GameObjectController>;

    [AddComponentMenu("TLab/SFU/Game Object Controller (TLab)")]
    public class GameObjectController : NetworkRigidbodyTransform
    {
        public enum HandType
        {
            None,
            First,
            Second,
        };

        public class GrabState
        {
            public static int FREE = -1;

            public enum Action
            {
                Grab,
                Free,
            };

            private int m_grabberId = FREE;

            public int grabberId => m_grabberId;

            public bool grabbed => m_grabberId != FREE;

            public bool free => !grabbed;

            public bool own => grabbed && NetworkClient.IsOwn(m_grabberId);

            public bool others => (grabbed && !own);

            public void Update(int grabberId) => m_grabberId = grabberId;

            public void Update(Action action)
            {
                switch (action)
                {
                    case Action.Grab:
                        m_grabberId = NetworkClient.userId;
                        break;
                    case Action.Free:
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

        private Interactor m_firstHand;
        private Interactor m_secondHand;

        public GrabState grabState => m_grabState;

        public bool locked => m_locked;

        public Interactor firstHand => m_firstHand;

        public Interactor secondHand => m_secondHand;

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

        public void AutoFitScaleLogicLim()
        {
            var localScale = this.transform.localScale;
            m_scale.scaleXLim = new Vector2(localScale.x * 0.5f, localScale.x * 2.0f);
            m_scale.scaleYLim = new Vector2(localScale.y * 0.5f, localScale.y * 2.0f);
            m_scale.scaleZLim = new Vector2(localScale.z * 0.5f, localScale.z * 2.0f);
        }
#endif

        private void OnFirstHandEnter()
        {
            m_position.OnFirstHandEnter(m_firstHand);
            m_rotation.OnFirstHandEnter(m_firstHand);
            m_scale.OnFirstHandEnter(m_firstHand);
        }

        private void OnSecondHandEnter()
        {
            m_position.OnSecondHandEnter(m_secondHand);
            m_rotation.OnSecondHandEnter(m_secondHand);
            m_scale.OnSecondHandEnter(m_secondHand);
        }

        private void OnFirstHandExit()
        {
            m_position.OnFirstHandExit(m_firstHand);
            m_rotation.OnFirstHandExit(m_firstHand);
            m_scale.OnFirstHandExit(m_firstHand);
        }

        private void OnSecondHandExit()
        {
            m_position.OnSecondHandExit(m_secondHand);
            m_rotation.OnSecondHandExit(m_secondHand);
            m_scale.OnSecondHandExit(m_secondHand);
        }

        #region MESSAGE

        [Serializable, Message(typeof(MSG_DivideGrabber))]
        public class MSG_DivideGrabber : Message
        {
            public Address64 networkId;
            public int grabberId;
            public bool active;

            public MSG_DivideGrabber(in Address64 networkId, int grabberId, bool active) : base()
            {
                this.networkId = networkId;
                this.grabberId = grabberId;
                this.active = active;
            }

            public MSG_DivideGrabber(byte[] bytes) : base(bytes) { }
        }

        [Serializable, Message(typeof(MSG_GrabbLock))]
        public class MSG_GrabbLock : Message
        {
            [System.Serializable]
            public enum Action
            {
                None,
                GrabLock,
                ForceRelease,
            };

            public Address64 networkId;
            public int grabberId;
            public Action action;

            public MSG_GrabbLock(in Address64 networkId, int grabberId, Action action) : base()
            {
                this.networkId = networkId;
                this.grabberId = grabberId;
                this.action = action;
            }

            public MSG_GrabbLock(byte[] bytes) : base(bytes) { }
        }

        #endregion MESSAGE

        public void GrabbLock(GrabState.Action action)
        {
            m_grabState.Update(action);

            switch (action)
            {
                case GrabState.Action.Grab:
                    EnableRigidbody(false);
                    StopInterpolation();
                    break;
                case GrabState.Action.Free:
                    EnableRigidbody(true);
                    break;
            }

            SyncViaWebSocket(NetworkClient.userId);

            NetworkClient.SendWS(new MSG_GrabbLock(m_networkId.id, m_grabState.grabberId, MSG_GrabbLock.Action.GrabLock).Marshall());
        }

        public override void OnRigidbodyModeChange()
        {
            switch (NetworkClient.rbMode)
            {
                case NetworkClient.RigidbodyMode.Send:
                    if (m_rbState.used)
                    {
                        m_rb.isKinematic = false;
                        m_rb.interpolation = m_rbInterpolation;
                    }

                    if (m_grabState.free)
                        EnableRigidbody(true);
                    break;
                case NetworkClient.RigidbodyMode.Recv:
                    if (m_rbState.used)
                    {
                        m_rb.isKinematic = true;
                        m_rb.interpolation = RigidbodyInterpolation.None;
                    }

                    EnableRigidbody(false, true);
                    break;
            }
        }

        public override void EnableRigidbody(bool enable, bool force = false)
        {
            var sender = NetworkClient.rbMode == NetworkClient.RigidbodyMode.Send;
            if (force || sender)
                base.EnableRigidbody(enable);
        }

        public void GrabbLock(int index)
        {
            if (index != GrabState.FREE)
            {
                if (m_firstHand != null)
                {
                    m_firstHand = null;
                    m_secondHand = null;
                }

                m_grabState.Update(index);

                EnableRigidbody(false);

                StopInterpolation();
            }
            else
            {
                m_grabState.Update(GrabState.Action.Free);

                EnableRigidbody(true);
            }
        }

        public void ForceRelease(bool self)
        {
            if (m_firstHand != null)
            {
                m_firstHand = null;
                m_secondHand = null;
                m_grabState.Update(GrabState.Action.Free);

                EnableRigidbody(false);
            }

            if (self)
                NetworkClient.SendWS(new MSG_GrabbLock(m_networkId.id, m_grabState.grabberId, MSG_GrabbLock.Action.ForceRelease).Marshall());
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
            if (m_firstHand == interactor)
                return HandType.First;

            if (m_secondHand == interactor)
                return HandType.Second;

            return HandType.None;
        }

        public HandType OnGrab(Interactor interactor)
        {
            if (m_locked || !initialized || m_grabState.others)
                return HandType.None;

            if (m_firstHand == null)
            {
                GrabbLock(GrabState.Action.Grab);

                m_firstHand = interactor;

                OnFirstHandEnter();

                return HandType.First;
            }
            else if (m_secondHand == null)
            {
                m_secondHand = interactor;

                OnSecondHandEnter();

                return HandType.Second;
            }

            return HandType.None;
        }

        public bool OnRelease(Interactor interactor)
        {
            if (m_firstHand == interactor)
            {
                OnFirstHandExit();

                if (m_secondHand != null)
                {
                    m_firstHand = m_secondHand;
                    m_secondHand = null;

                    OnFirstHandEnter();

                    return true;
                }
                else
                {
                    GrabbLock(GrabState.Action.Free);

                    m_firstHand = null;

                    return false;
                }
            }
            else if (m_secondHand == interactor)
            {
                OnSecondHandExit();

                m_secondHand = null;

                OnFirstHandEnter();

                return false;
            }

            return false;
        }

        protected override void BeforeShutdown()
        {
            if (m_grabState.own)
                GrabbLock(GrabState.Action.Free);

            base.BeforeShutdown();
        }

        protected override void RegisterOnMessage()
        {
            base.RegisterOnMessage();

            NetworkClient.RegisterOnMessage<MSG_GrabbLock>((from, to, bytes) =>
            {
                var receive = new MSG_GrabbLock(bytes);

                switch (receive.action)
                {
                    case MSG_GrabbLock.Action.GrabLock:
                        Registry.GetByKey(receive.networkId)?.GrabbLock(receive.grabberId);
                        break;
                    case MSG_GrabbLock.Action.ForceRelease:
                        Registry.GetByKey(receive.networkId)?.ForceRelease(false);
                        break;
                    default:
                        break;
                }
            });

            NetworkClient.RegisterOnMessage<MSG_DivideGrabber>((from, to, bytes) =>
            {
                var receive = new MSG_DivideGrabber(bytes);
                Registry.GetByKey(receive.networkId)?.Divide(receive.active);
            });
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
            if (m_firstHand != null)
            {
                if (m_secondHand != null)
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
            else if (m_grabState.free)
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