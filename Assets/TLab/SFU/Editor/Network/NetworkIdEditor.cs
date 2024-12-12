using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkId), true), CanEditMultipleObjects]
    public class NetworkIdEditor : UnityEditor.Editor
    {
        private NetworkId m_instance;

        private void OnEnable() => m_instance = target as NetworkId;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            GUILayout.Label($"Hash: {m_instance.id.hash}", GUILayout.ExpandWidth(false));
            EditorGUILayout.Space();
        }
    }
}
