using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLab.VRProjct.Test.Editor
{
    [CustomEditor(typeof(TestCode))]
    public class TestCodeEditor : UnityEditor.Editor
    {
        private TestCode instance;

        private void OnEnable() => instance = target as TestCode;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Json Utility Test"))
                instance.JsonUtilityTest();

            if (GUILayout.Button("Collider Test"))
                instance.MeshColliderClosestPoint();
        }
    }
}
#endif