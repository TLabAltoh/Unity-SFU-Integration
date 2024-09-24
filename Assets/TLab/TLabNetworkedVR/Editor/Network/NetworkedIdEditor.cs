using UnityEngine;
using UnityEditor;

namespace TLab.NetworkedVR.Network.Editor
{
    [CustomEditor(typeof(NetworkedId))]
    public class NetworkedIdEditor : UnityEditor.Editor
    {
        private NetworkedId m_instance;

        private void OnEnable()
        {
            m_instance = target as NetworkedId;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Create Hash ID"))
            {
                m_instance.CreateHashID();
                EditorUtility.SetDirty(m_instance);
            }
        }
    }
}
