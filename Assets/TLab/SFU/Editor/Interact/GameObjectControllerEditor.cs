using UnityEngine;
using UnityEditor;

using Oculus.Interaction.Surfaces;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(GameObjectController))]
    [CanEditMultipleObjects]
    public class GameObjectControllerEditor : UnityEditor.Editor
    {
        private GameObjectController m_controller;

        private void OnEnable()
        {
            m_controller = target as GameObjectController;
        }

        private void InitializeForRotateble(GameObjectController controller)
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
            controller.SetSyncEnable(true);
            controller.UseRigidbody(false, false);  // Disable Rigidbody.useGrabity

            var goGrabbable = target.RequireComponent<GameObjectGrabbable>();
            goGrabbable.enableCollision = true;

            var goRotatable = target.RequireComponent<GameObjectRotatable>();
            goRotatable.enableCollision = true;

            var rayInteractable = target.RequireComponent<RayInteractable>();
            var colliderSurface = target.RequireComponent<ColliderSurface>();

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(meshCollider);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(goRotatable);
            EditorUtility.SetDirty(goGrabbable);

            EditorUtility.SetDirty(rayInteractable);
            EditorUtility.SetDirty(colliderSurface);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var goRotatable = m_controller.gameObject.GetComponent<GameObjectRotatable>();
            if (goRotatable != null && GUILayout.Button("Initialize for GameObjectRotatable"))
            {
                InitializeForRotateble(m_controller);
            }

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
                GUILayout.Label($"current grabber id: {m_controller.grabState.grabberId}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
        }
    }
}
