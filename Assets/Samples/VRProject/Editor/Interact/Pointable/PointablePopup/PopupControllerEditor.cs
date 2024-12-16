using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(PopupController))]
    public class PopupControllerEditor : UnityEditor.Editor
    {
        private void Convert2PointablePopup(int index, ref PopupController controller)
        {
            const string NAME = "[" + nameof(PopupController) + "] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = controller.pointerPairs[index].target;

            var pointableOutline = target.GetComponent<PointableOutline>();
            var pointablePopup = target.GetComponent<PointablePopup>();
            if (pointablePopup == null || pointablePopup == pointableOutline)
                pointablePopup = target.AddComponent<PointablePopup>();

            if (pointablePopup != null)
            {
                pointablePopup.material = pointableOutline.material;
                pointablePopup.controller = controller;
                pointablePopup.index = index;
                pointablePopup.collision.enabled = false;
                DestroyImmediate(pointableOutline);
                EditorUtility.SetDirty(pointablePopup);
                Debug.Log(NAME + "Convert 2 " + nameof(PointablePopup) + " " + index.ToString());
            }

            Debug.Log(NAME + BAR);
        }

        private void Convert2PointableOutline(int index, ref PopupController controller)
        {
            const string NAME = "[" + nameof(PopupController) + "] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = controller.pointerPairs[index].target;

            var pointableOutline = target.GetComponent<PointableOutline>();
            var pointablePopup = target.GetComponent<PointablePopup>();

            if (pointableOutline == null || pointableOutline == pointablePopup)
                pointableOutline = target.AddComponent<PointableOutline>();

            if (pointablePopup != null)
            {
                Debug.Log(NAME + "Convert 2 " + nameof(PointableOutline) + " " + index.ToString());
                pointableOutline.material = pointablePopup.material;
                DestroyImmediate(pointablePopup);
            }

            if (pointableOutline != null)
            {
                pointablePopup.collision.enabled = false;
                EditorUtility.SetDirty(pointableOutline);
            }

            Debug.Log(NAME + BAR);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var controller = target as PopupController;

            EditorGUILayout.Space();
            GUILayout.Label($"Convert 2: ", GUILayout.ExpandWidth(false));
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(nameof(PointablePopup)))
            {
                for (int index = 0; index < controller.pointerPairs.Length; index++)
                    Convert2PointablePopup(index, ref controller);
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button(nameof(PointableOutline)))
            {
                for (int index = 0; index < controller.pointerPairs.Length; index++)
                    Convert2PointableOutline(index, ref controller);
                EditorUtility.SetDirty(controller);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}