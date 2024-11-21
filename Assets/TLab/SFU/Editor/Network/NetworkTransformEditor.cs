using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkTransform), true)]
    public class NetworkTransformEditor : NetworkObjectEditor
    {
        private NetworkTransform m_transform;

        protected override void Init()
        {
            base.Init();

            m_transform = target as NetworkTransform;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Rigidbody Used: {m_transform.rbState.used}, Gravity: {m_transform.rbState.gravity}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }
        }
    }
}
