using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkObjectGroup)), CanEditMultipleObjects]
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

            if (GUILayout.Button("Set Private For All Child"))
            {
                m_instance.SetPrivateForAllChild();
                EditorUtility.SetDirty(m_instance);
            }
        }
    }
}
