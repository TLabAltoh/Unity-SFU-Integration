using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkObjectGroup))]
    public class NetworkObjectGroupEditor : UnityEditor.Editor
    {
        private NetworkObjectGroup m_instance;

        private void OnEnable() => m_instance = target as NetworkObjectGroup;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            GUILayout.Label($"Lenght: {m_instance.length}", GUILayout.ExpandWidth(false));
            EditorGUILayout.Space();

            if (GUILayout.Button("Update Registry"))
            {
                m_instance.UpdateRegistry();
                EditorUtility.SetDirty(m_instance);
            }
        }
    }
}
