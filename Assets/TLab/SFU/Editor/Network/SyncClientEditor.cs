using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(SyncClient))]
    public class SyncClientEditor : UnityEditor.Editor
    {
        private SyncClient m_instance;

        private void OnEnable() => m_instance = target as SyncClient;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Test"))
            {

            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"My user id: {SyncClient.userId}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }
        }
    }
}
