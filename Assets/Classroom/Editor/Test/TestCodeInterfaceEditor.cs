using UnityEngine;
using UnityEditor;

namespace TLab.VRClassroom
{
    [CustomEditor(typeof(TestCodeInterface))]
    public class TestCodeInterfaceEditor : UnityEditor.Editor
    {
        private TestCodeInterface instance;

        private void OnEnable()
        {
            instance = target as TestCodeInterface;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Array Instantiate Test"))
            {
                instance.ArrayInstantiateTest();
            }

            if (GUILayout.Button("Collider Test"))
            {
                instance.MeshColliderClosestPoint();
            }
        }
    }
}
