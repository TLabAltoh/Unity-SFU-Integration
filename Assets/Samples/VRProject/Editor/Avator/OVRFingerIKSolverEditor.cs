using UnityEditor;
using UnityEngine;

namespace TLab.VRProjct.Avator.Editor
{
    [CustomEditor(typeof(OVRFingerIKSolver)), CanEditMultipleObjects]
    public class OVRFingerIKSolverEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            OVRFingerIKSolver solver = (OVRFingerIKSolver)target;
            if (solver.needResetOption)
            {
                GUI.enabled = false;
            }
            DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Reset Controls"))
                    solver.ResetControls();
            }

            if (solver.needResetOption)
            {
                GUI.enabled = true;
                if (GUILayout.Button("Reset Scene Hierarchy"))
                    solver.ResetHierarchy();
            }
        }
    }
}