using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU.Humanoid
{
    [AddComponentMenu("TLab/SFU/Body Tracker (TLab)")]
    public class BodyTracker : SyncTransformer
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [System.Serializable]
        public class TrackTarget
        {
            public AvatorConfig.PartsType parts;

            public Transform target;
        }

        [SerializeField] private AvatorConfig.PartsType m_partsType;

        [SerializeField] private bool m_self = false;

        [SerializeField] private bool m_initializeOnStartUp = false;

        private bool m_initialized = false;

        public void Destroy() => Registry<BodyTracker>.UnRegister(m_networkedId.id);

        public void Init(AvatorConfig.PartsType partsType, bool self)
        {
            if (m_initialized)
            {
                Debug.LogError(THIS_NAME + "This tracker has already been initialised.");

                return;
            }

            m_partsType = partsType;

            m_self = self;

            m_initialized = true;
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            if (m_initializeOnStartUp)
            {
                Init(m_partsType, m_self);
            }

            base.Start();

            Registry<BodyTracker>.Register(m_networkedId.id, this);
        }

        protected override void Update()
        {
            base.Update();

            if (m_initialized && m_self)
            {
                SyncViaWebRTC();
            }
        }

        protected override void OnApplicationQuit()
        {
            Shutdown();

            base.OnApplicationQuit();
        }

        protected override void OnDestroy()
        {
            Shutdown();

            base.OnDestroy();
        }
    }
}
