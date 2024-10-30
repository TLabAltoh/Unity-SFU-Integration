using UnityEngine;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/SimpleTracker (TLab)")]
    public class SimpleTracker : SyncTransformer
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        private bool m_self = false;

        public bool self { get => m_self; set => m_self = value; }

        public override void Shutdown() => Registry<SimpleTracker>.UnRegister(m_networkedId.id);

        protected override void Start()
        {
            base.Start();

            Registry<SimpleTracker>.Register(m_networkedId.id, this);
        }

        protected override void Update()
        {
            base.Update();

            if (m_self)
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
