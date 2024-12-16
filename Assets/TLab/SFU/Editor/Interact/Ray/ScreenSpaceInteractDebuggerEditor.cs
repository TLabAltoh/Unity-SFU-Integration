using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(ScreenSpaceInteractDebugger))]
    public class ScreenSpaceInteractDebuggerEditor : UnityEditor.Editor
    {
        private ScreenSpaceInteractDebugger m_interactor;

        private void OnEnable()
        {
            m_interactor = target as ScreenSpaceInteractDebugger;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginHorizontal();

            var width = GUILayout.Width(Screen.width / 3);

            if (GUILayout.Button("Grab", width))
            {
                m_interactor.Grab();
            }

            if (GUILayout.Button("Release", width))
            {
                m_interactor.Release();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
