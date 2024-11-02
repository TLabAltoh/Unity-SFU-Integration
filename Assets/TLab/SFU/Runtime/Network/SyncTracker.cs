using UnityEngine;

namespace TLab.SFU.Network
{
    using Registry = Registry<SyncTracker>;

    [AddComponentMenu("TLab/SFU/SyncTracker (TLab)")]
    public class SyncTracker : SyncTransformer
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        private bool m_self = false;

        public bool self { get => m_self; set => m_self = value; }

        public override void Shutdown()
        {
            if (m_networkedId)
                Registry.UnRegister(m_networkedId.id);

            base.Shutdown();
        }

        public override void Init()
        {
            base.Init();

            Registry.Register(m_networkedId.id, this);
        }

        public override void Init(Address32 publicId)
        {
            base.Init(publicId);

            Registry.Register(m_networkedId.id, this);
        }

        protected override void Register()
        {
            base.Register();

            Registry.Register(m_networkedId.id, this);
        }

        protected override void UnRegister()
        {
            Registry.UnRegister(m_networkedId.id);

            base.UnRegister();
        }

        protected override void Update()
        {
            base.Update();

            if (m_self)
                SyncViaWebRTC();
        }
    }
}
