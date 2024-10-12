using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(SyncTransformer))]
    public class SyncTransformerEditor : UnityEditor.Editor
    {
        private SyncTransformer m_instance;

        private void OnEnable()
        {
            m_instance = target as SyncTransformer;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}
