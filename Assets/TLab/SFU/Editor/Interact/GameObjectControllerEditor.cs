using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(GameObjectController))]
    [CanEditMultipleObjects]
    public class GameObjectControllerEditor : UnityEditor.Editor
    {
        private GameObjectController m_controller;

        private void OnEnable() => m_controller = target as GameObjectController;

        private void InitializeForRotatable(GameObjectController controller)
        {
            controller.InitializeGameObjectRotatable();
            EditorUtility.SetDirty(controller);
        }

        private void InitializeForDivibable(GameObject target, bool isRoot)
        {
            var meshFilter = target.RequireComponent<MeshFilter>();

            var meshCollider = target.RequireComponent<MeshCollider>();
            meshCollider.enabled = isRoot;
            meshCollider.convex = true;     // meshCollider.ClosestPoint only works with convex = true

            var controller = target.RequireComponent<GameObjectController>();
            controller.direction = Network.Direction.SENDRECV;
            controller.UseRigidbody(false, false);  // Disable Rigidbody.useGrabity

            var goGrabbable = target.RequireComponent<GameObjectGrabbable>();
            goGrabbable.enableCollision = true;

            var rotatable = target.RequireComponent<GameObjectRotatable>();
            rotatable.enableCollision = true;

            var rayInteractable = target.RequireComponent<RayInteractable>();

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(meshCollider);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(rotatable);
            EditorUtility.SetDirty(goGrabbable);

            EditorUtility.SetDirty(rayInteractable);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var rotatable = m_controller.gameObject.GetComponent<GameObjectRotatable>();
            if (rotatable != null && GUILayout.Button("Initialize for GameObjectRotatable"))
                InitializeForRotatable(m_controller);

            if (m_controller.enableDivide && GUILayout.Button("Initialize for Devibable"))
            {
                InitializeForDivibable(m_controller.gameObject, true);

                foreach (var divideTarget in m_controller.divideTargets)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(divideTarget);

                    InitializeForDivibable(divideTarget, false);

                    EditorUtility.SetDirty(divideTarget);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Current Grabber Id: {m_controller.grabState.grabberId}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
        }
    }
}
