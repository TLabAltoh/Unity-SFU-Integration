using UnityEngine;
using UnityEditor;

namespace TLab.VRClassroom
{
    [CustomEditor(typeof(CodeTester))]
    public class CodeTesterEditor : UnityEditor.Editor
    {
        private CodeTester instance;

        private void OnEnable()
        {
            instance = target as CodeTester;
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
