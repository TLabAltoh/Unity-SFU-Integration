using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(PopupController))]
    public class PopupControllerEditor : UnityEditor.Editor
    {
        private void Convert2PointablePopup(int index, ref PopupController popupController)
        {
            const string NAME = "[" + nameof(PopupController) + "] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = popupController.pointerPairs[index].target;

            var pointableOutline = target.GetComponent<PointableOutline>();
            var pointablePopup = target.GetComponent<PointablePopup>();
            if (pointablePopup == null || pointablePopup == pointableOutline)
                pointablePopup = target.AddComponent<PointablePopup>();

            if (pointablePopup != null)
            {
                pointablePopup.outlineMat = pointableOutline.outlineMat;
                pointablePopup.popupController = popupController;
                pointablePopup.index = index;
                pointablePopup.enableCollision = false;
                DestroyImmediate(pointableOutline);
                EditorUtility.SetDirty(pointablePopup);
                Debug.Log(NAME + "Convert 2 " + nameof(PointablePopup) + " " + index.ToString());
            }

            Debug.Log(NAME + BAR);
        }

        private void Convert2PointableOutline(int index, ref PopupController popupController)
        {
            const string NAME = "[" + nameof(PopupController) + "] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = popupController.pointerPairs[index].target;

            var pointableOutline = target.GetComponent<PointableOutline>();
            var pointablePopup = target.GetComponent<PointablePopup>();

            if (pointableOutline == null || pointableOutline == pointablePopup)
                pointableOutline = target.AddComponent<PointableOutline>();

            if (pointablePopup != null)
            {
                Debug.Log(NAME + "Convert 2 " + nameof(PointableOutline) + " " + index.ToString());
                pointableOutline.outlineMat = pointablePopup.outlineMat;
                DestroyImmediate(pointablePopup);
            }

            if (pointableOutline != null)
            {
                pointableOutline.enableCollision = false;
                EditorUtility.SetDirty(pointableOutline);
            }

            Debug.Log(NAME + BAR);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var popupController = target as PopupController;

            EditorGUILayout.Space();
            GUILayout.Label($"Convert 2: ", GUILayout.ExpandWidth(false));
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(nameof(PointablePopup)))
            {
                for (int index = 0; index < popupController.pointerPairs.Length; index++)
                    Convert2PointablePopup(index, ref popupController);
                EditorUtility.SetDirty(popupController);
            }

            if (GUILayout.Button(nameof(PointableOutline)))
            {
                for (int index = 0; index < popupController.pointerPairs.Length; index++)
                    Convert2PointableOutline(index, ref popupController);
                EditorUtility.SetDirty(popupController);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}