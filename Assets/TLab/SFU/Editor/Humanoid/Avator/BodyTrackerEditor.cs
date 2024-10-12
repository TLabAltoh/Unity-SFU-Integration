using UnityEditor;

namespace TLab.SFU.Humanoid
{
    [CustomEditor(typeof(BodyTracker))]
    public class BodyTrackerEditor : UnityEditor.Editor
    {
        private BodyTracker m_instance;

        private void OnEnable()
        {
            m_instance = target as BodyTracker;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}
