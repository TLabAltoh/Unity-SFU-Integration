using UnityEngine;
using UnityEditor;

namespace TLab.NetworkedVR.Network.Editor
{
    [CustomEditor(typeof(PrefabStore))]
    public class PrefabStoreEditor : UnityEditor.Editor
    {
        private PrefabStore m_instance;

        private int m_prefabIndex = 0;

        private int m_userIndex = 0;

        private GUIContent[] m_options = new[]
        {
            new GUIContent("0"),
            new GUIContent("1"),
            new GUIContent("2"),
        };

        private void OnEnable()
        {
            m_instance = target as PrefabStore;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();

                m_prefabIndex = EditorGUILayout.Popup(
                    label: new GUIContent("Prefab Index"),
                    selectedIndex: m_prefabIndex, m_options);

                EditorGUILayout.Space();

                m_userIndex = EditorGUILayout.Popup(
                    label: new GUIContent("User Index"),
                    selectedIndex: m_prefabIndex, m_options);

                EditorGUILayout.Space();

                using (var horizontalScope = new GUILayout.HorizontalScope("box"))
                {
                    if (GUILayout.Button("Instantiate"))
                    {
                        // TODO:
                    }

                    if (GUILayout.Button("Destroy"))
                    {
                        // TODO:
                    }
                }
            }
        }
    }
}
