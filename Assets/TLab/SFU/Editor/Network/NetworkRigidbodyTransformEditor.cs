using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkRigidbodyTransform), true)]
    public class NetworkRigidbodyTransformEditor : NetworkObjectEditor
    {
        private NetworkRigidbodyTransform m_transform;

        protected override void Init()
        {
            base.Init();

            m_transform = target as NetworkRigidbodyTransform;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Rigidbody Active: {m_transform.rbState.active}, Gravity: {m_transform.rbState.gravity}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }
        }
    }
}
