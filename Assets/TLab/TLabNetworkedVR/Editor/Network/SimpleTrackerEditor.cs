using UnityEngine;
using UnityEditor;

namespace TLab.NetworkedVR.Network.Editor
{
    [CustomEditor(typeof(SimpleTracker))]
    public class SimpleTrackerEditor : UnityEditor.Editor
    {
        private SimpleTracker m_instance;

        private void OnEnable()
        {
            m_instance = target as SimpleTracker;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}
