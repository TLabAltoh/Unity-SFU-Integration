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

        private void OnEnable()
        {
            instance = target as TestCode;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Array Instantiate Test"))
                instance.ArrayInstantiateTest();

            if (GUILayout.Button("Collider Test"))
                instance.MeshColliderClosestPoint();

            if (GUILayout.Button("Command Parse Test"))
            {
                var (program, argments) = CommandLine.Parse("192.168.3.11 -p 1234 -t 3333");

                Debug.Log($"Program: {program}");

                foreach (var argment in argments)
                    Debug.Log($"Argments: {argment.Key}, {argment.Value}");
            }

        }
    }
}
#endif