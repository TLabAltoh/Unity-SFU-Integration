using UnityEngine;
using UnityEditor;

namespace TLab.NetworkedVR.Interact.Editor
{
    [CustomEditor(typeof(ScreenSpaceRayInteractor))]
    public class ScreenSpaceRayInteractorEditor : UnityEditor.Editor
    {
        private ScreenSpaceRayInteractor m_interactor;

        private void OnEnable()
        {
            m_interactor = target as ScreenSpaceRayInteractor;
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
