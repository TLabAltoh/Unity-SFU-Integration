using UnityEngine;
using UnityEditor;
using TLab.SFU.Network.Editor;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(GameObjectController), true)]
    [CanEditMultipleObjects]
    public class GameObjectControllerEditor : NetworkRigidbodyTransformEditor
    {
        private GameObjectController m_controller;

        protected override void Init()
        {
            base.Init();

            m_controller = target as GameObjectController;
        }

        private void InitGameObjectRotatable(GameObjectController controller)
        {
            controller.InitializeGameObjectRotatable();
            EditorUtility.SetDirty(controller);
        }

        private void InitDivideTarget(GameObject target, bool isRoot)
        {
            target.RequireComponent<MeshFilter>((c) => EditorUtility.SetDirty(c));

            var meshCollider = target.RequireComponent<MeshCollider>((c) => {
                c.enabled = isRoot;
                c.convex = true;    // meshCollider.ClosestPoint only works with convex = true
                EditorUtility.SetDirty(c);
            });

            target.RequireComponent<GameObjectController>((c) => {
                c.direction = Network.Direction.SendRecv;
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

            EditorGUILayout.Space();
            GUILayout.Label($"Initialize Component: ", GUILayout.ExpandWidth(false));
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            var rotatable = m_controller.gameObject.GetComponent<GameObjectRotatable>();
            if (rotatable != null && GUILayout.Button(nameof(GameObjectRotatable)))
                InitGameObjectRotatable(m_controller);

            if (m_controller.enableDivide && GUILayout.Button("Divide Target"))
            {
                InitDivideTarget(m_controller.gameObject, true);

                foreach (var divideTarget in m_controller.divideTargets)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(divideTarget);

                    InitDivideTarget(divideTarget, false);

                    EditorUtility.SetDirty(divideTarget);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.Label($"Initialize Transform: ", GUILayout.ExpandWidth(false));
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Auto Fit" + nameof(ScaleLogic) + " Lim"))
            {
                m_controller.AutoFitScaleLogicLim();
                EditorUtility.SetDirty(m_controller);
            }

            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Current Grabber Id: {m_controller.grabState.grabberId}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }
        }
    }
}
