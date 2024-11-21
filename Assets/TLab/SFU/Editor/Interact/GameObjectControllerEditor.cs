using UnityEngine;
using UnityEditor;
using TLab.SFU.Network.Editor;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(GameObjectController), true)]
    [CanEditMultipleObjects]
    public class GameObjectControllerEditor : NetworkTransformEditor
    {
        private GameObjectController m_controller;

        protected override void Init()
        {
            base.Init();

            m_controller = target as GameObjectController;
        }

        private void InitRotatable(GameObjectController controller)
        {
            controller.InitializeGameObjectRotatable();
            EditorUtility.SetDirty(controller);
        }

        private void InitDivibable(GameObject target, bool isRoot)
        {
            target.RequireComponent<MeshFilter>((c) => EditorUtility.SetDirty(c));

            var meshCollider = target.RequireComponent<MeshCollider>((c) => {
                c.enabled = isRoot;
                c.convex = true;    // meshCollider.ClosestPoint only works with convex = true
                EditorUtility.SetDirty(c);
            });

            target.RequireComponent<GameObjectController>((c) => {
                c.direction = Network.Direction.SENDRECV;
                c.UseRigidbody(false, false);
                EditorUtility.SetDirty(c);
            });

            target.RequireComponent<GameObjectGrabbable>((c) => {
                c.enableCollision = true;
                EditorUtility.SetDirty(c);
            });

            target.RequireComponent<GameObjectRotatable>((c) => {
                c.enableCollision = true;
                EditorUtility.SetDirty(c);
            });

            target.RequireComponent<RayInteractable>((c) => EditorUtility.SetDirty(c));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var rotatable = m_controller.gameObject.GetComponent<GameObjectRotatable>();
            if (rotatable != null && GUILayout.Button("Init Rotatable"))
                InitRotatable(m_controller);

            if (m_controller.enableDivide && GUILayout.Button("Init Devibable"))
            {
                InitDivibable(m_controller.gameObject, true);

                foreach (var divideTarget in m_controller.divideTargets)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(divideTarget);

                    InitDivibable(divideTarget, false);

                    EditorUtility.SetDirty(divideTarget);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Current Grabber Id: {m_controller.grabState.grabberId}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }
        }
    }
}
